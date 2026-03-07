import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios';

const API_BASE_URL = 'https://salonpro.runasp.net/api';

const TOKEN_KEY = 'salonpro_token';
const REFRESH_TOKEN_KEY = 'salonpro_refresh_token';
const TENANT_KEY = 'salonpro_tenant_id';

/** Ensure tenant id is a valid GUID string (with dashes) so backend parses it correctly */
function normalizeTenantId(value: string | null): string | null {
  if (!value || typeof value !== 'string') return null;
  const trimmed = value.trim();
  if (!trimmed) return null;
  if (/^[0-9a-fA-F-]{36}$/.test(trimmed)) return trimmed;
  const hex = trimmed.replace(/-/g, '');
  if (/^[0-9a-fA-F]+$/.test(hex) && hex.length <= 32) {
    const padded = hex.padStart(32, '0');
    return `${padded.slice(0, 8)}-${padded.slice(8, 12)}-${padded.slice(12, 16)}-${padded.slice(16, 20)}-${padded.slice(20, 32)}`;
  }
  return trimmed;
}

export const getStoredToken = (): string | null => localStorage.getItem(TOKEN_KEY);
export const getStoredRefreshToken = (): string | null => localStorage.getItem(REFRESH_TOKEN_KEY);
export const getStoredTenantId = (): string | null => localStorage.getItem(TENANT_KEY);

export const setStoredToken = (token: string): void => localStorage.setItem(TOKEN_KEY, token);
export const setStoredRefreshToken = (token: string): void => localStorage.setItem(REFRESH_TOKEN_KEY, token);
export const setStoredTenantId = (tenantId: string): void => localStorage.setItem(TENANT_KEY, tenantId);

export const clearStoredAuth = (): void => {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
  localStorage.removeItem(TENANT_KEY);
};

// Decode JWT payload
export const decodeJwtPayload = (token: string): Record<string, unknown> => {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    return JSON.parse(jsonPayload);
  } catch {
    return {};
  }
};

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor: attach auth headers
apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = getStoredToken();
  const tenantId = normalizeTenantId(getStoredTenantId());
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  if (tenantId) {
    config.headers['X-Tenant-Id'] = tenantId;
  }
  return config;
});

let isRefreshing = false;
let failedQueue: Array<{
  resolve: (token: string) => void;
  reject: (err: unknown) => void;
}> = [];

const processQueue = (error: unknown, token: string | null = null) => {
  failedQueue.forEach(prom => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token!);
    }
  });
  failedQueue = [];
};

// Response interceptor: handle 401 → try refresh token
apiClient.interceptors.response.use(
  response => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        }).then(token => {
          originalRequest.headers.Authorization = `Bearer ${token}`;
          return apiClient(originalRequest);
        });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const refreshToken = getStoredRefreshToken();
      const accessToken = getStoredToken();

      if (!refreshToken || !accessToken) {
        clearStoredAuth();
        window.location.href = '/';
        return Promise.reject(error);
      }

      try {
        const response = await axios.post(`${API_BASE_URL}/auth/refresh-token`, {
          accessToken,
          refreshToken,
        });

        const { accessToken: newToken, refreshToken: newRefresh } = response.data;
        setStoredToken(newToken);
        setStoredRefreshToken(newRefresh);

        processQueue(null, newToken);
        originalRequest.headers.Authorization = `Bearer ${newToken}`;
        return apiClient(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError, null);
        clearStoredAuth();
        window.location.href = '/';
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);

export default apiClient;
