import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import type { AuthUser, AuthResponse, TenantFeatures, TenantPlan } from '../types';
import { login as apiLogin, register as apiRegister } from '../api/auth';
import type { LoginRequest, RegisterRequest } from '../types';
import {
  getStoredToken,
  getStoredTenantId,
  setStoredToken,
  setStoredRefreshToken,
  setStoredTenantId,
  clearStoredAuth,
  decodeJwtPayload,
} from '../api/client';

interface AuthContextType {
  user: AuthUser | null;
  plan: TenantPlan;
  features: TenantFeatures;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (data: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<AuthResponse>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | null>(null);

const AUTH_META_KEY = 'salonpro_auth_meta';
const defaultFeatures: TenantFeatures = {
  canUseOnlineBooking: false,
  maxStaffMembers: 1,
};

type StoredAuthMeta = {
  plan?: TenantPlan;
  features?: TenantFeatures;
};

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [plan, setPlan] = useState<TenantPlan>('Basic');
  const [features, setFeatures] = useState<TenantFeatures>(defaultFeatures);
  const [isLoading, setIsLoading] = useState(true);

  // Restore session from localStorage
  useEffect(() => {
    const token = getStoredToken();
    const tenantId = getStoredTenantId();
    if (token && tenantId) {
      try {
        const storedMetaRaw = localStorage.getItem(AUTH_META_KEY);
        if (storedMetaRaw) {
          const parsed = JSON.parse(storedMetaRaw) as StoredAuthMeta;
          if (parsed.plan) setPlan(parsed.plan);
          if (parsed.features) setFeatures(parsed.features);
        }

        const payload = decodeJwtPayload(token);
        const exp = payload['exp'] as number | undefined;
        if (exp && exp * 1000 < Date.now()) {
          clearStoredAuth();
        } else {
          // Helper: .NET JWT uses full URI claim names, so check both short and long forms
          const claim = (short: string, uri: string) =>
            (payload[short] ?? payload[uri] ?? '') as string;

          setUser({
            id: claim('sub', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'),
            email: claim('email', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'),
            name: claim('name', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'),
            role: claim('role', 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'),
            tenantId,
            tenantName: claim('tenantName', 'tenant_name'),
          });
        }
      } catch {
        clearStoredAuth();
        localStorage.removeItem(AUTH_META_KEY);
      }
    }
    setIsLoading(false);
  }, []);

  const login = useCallback(async (data: LoginRequest) => {
    const response = await apiLogin(data);
    setStoredToken(response.accessToken);
    setStoredRefreshToken(response.refreshToken);
    setStoredTenantId(response.user.tenantId);
    setUser(response.user);
    setPlan(response.plan ?? 'Basic');
    setFeatures(response.features ?? defaultFeatures);
    localStorage.setItem(AUTH_META_KEY, JSON.stringify({
      plan: response.plan ?? 'Basic',
      features: response.features ?? defaultFeatures,
    }));
  }, []);

  const register = useCallback(async (data: RegisterRequest): Promise<AuthResponse> => {
    const response = await apiRegister(data);
    // Don't store tokens or set user if email verification is required
    if (!response.requiresEmailVerification && response.accessToken) {
      setStoredToken(response.accessToken);
      setStoredRefreshToken(response.refreshToken);
      setStoredTenantId(response.user.tenantId);
      setUser(response.user);
      setPlan(response.plan ?? 'Basic');
      setFeatures(response.features ?? defaultFeatures);
      localStorage.setItem(AUTH_META_KEY, JSON.stringify({
        plan: response.plan ?? 'Basic',
        features: response.features ?? defaultFeatures,
      }));
    }
    return response;
  }, []);

  const logout = useCallback(() => {
    clearStoredAuth();
    setUser(null);
    setPlan('Basic');
    setFeatures(defaultFeatures);
    localStorage.removeItem(AUTH_META_KEY);
  }, []);

  const value: AuthContextType = {
    user,
    plan,
    features,
    isLoading,
    isAuthenticated: user !== null,
    login,
    register,
    logout,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = (): AuthContextType => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
};
