import apiClient from './client';
import type { AuthResponse, LoginRequest, RegisterRequest, RefreshTokenRequest, RefreshTokenResponse } from '../types';

export const login = async (data: LoginRequest): Promise<AuthResponse> => {
  const response = await apiClient.post<AuthResponse>('/auth/login', data);
  return response.data;
};

export const register = async (data: RegisterRequest): Promise<AuthResponse> => {
  const body = {
    tenantName: data.tenantName,
    tenantSlug: data.tenantSlug ?? data.tenantName.trim().toLowerCase().replace(/\s+/g, '-').replace(/[^a-z0-9-]/g, ''),
    email: data.email,
    password: data.password,
    firstName: data.firstName,
    lastName: data.lastName,
  };
  const response = await apiClient.post<AuthResponse>('/auth/register', body);
  return response.data;
};

export const refreshToken = async (data: RefreshTokenRequest): Promise<RefreshTokenResponse> => {
  const response = await apiClient.post<RefreshTokenResponse>('/auth/refresh-token', data);
  return response.data;
};
