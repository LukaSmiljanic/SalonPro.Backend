import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import type { AuthUser } from '../types';
import { login as apiLogin } from '../api/auth';
import type { LoginRequest } from '../types';
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
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (data: LoginRequest) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | null>(null);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Restore session from localStorage
  useEffect(() => {
    const token = getStoredToken();
    const tenantId = getStoredTenantId();
    if (token && tenantId) {
      try {
        const payload = decodeJwtPayload(token);
        const exp = payload['exp'] as number | undefined;
        if (exp && exp * 1000 < Date.now()) {
          clearStoredAuth();
        } else {
          setUser({
            id: payload['sub'] as string ?? '',
            email: payload['email'] as string ?? '',
            name: payload['name'] as string ?? '',
            role: payload['role'] as string ?? '',
            tenantId,
            tenantName: payload['tenantName'] as string ?? '',
          });
        }
      } catch {
        clearStoredAuth();
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
  }, []);

  const logout = useCallback(() => {
    clearStoredAuth();
    setUser(null);
  }, []);

  const value: AuthContextType = {
    user,
    isLoading,
    isAuthenticated: user !== null,
    login,
    logout,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = (): AuthContextType => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
};
