import apiClient from './client';
import type { WorkingHoursEntry, LoyaltyTierConfig, TenantBranding, UpdateTenantBrandingRequest } from '../types';

export const getWorkingHours = async (): Promise<WorkingHoursEntry[]> => {
  const response = await apiClient.get<WorkingHoursEntry[]>('/settings/working-hours');
  return response.data ?? [];
};

export const updateWorkingHours = async (data: WorkingHoursEntry[]): Promise<void> => {
  await apiClient.put('/settings/working-hours', data);
};

export const getLoyaltyConfig = async (): Promise<LoyaltyTierConfig[]> => {
  const response = await apiClient.get<LoyaltyTierConfig[]>('/settings/loyalty-tiers');
  return response.data ?? [];
};

export const updateLoyaltyConfig = async (data: LoyaltyTierConfig[]): Promise<void> => {
  await apiClient.put('/settings/loyalty-tiers', data);
};

export const getTenantBranding = async (): Promise<TenantBranding> => {
  const response = await apiClient.get<TenantBranding>('/settings/branding');
  return response.data;
};

export const updateTenantBranding = async (data: UpdateTenantBrandingRequest): Promise<TenantBranding> => {
  const response = await apiClient.put<TenantBranding>('/settings/branding', data);
  return response.data;
};

export const uploadTenantLogo = async (file: File): Promise<TenantBranding> => {
  const form = new FormData();
  form.append('file', file);
  const response = await apiClient.post<TenantBranding>('/settings/branding/logo', form, {
    headers: { 'Content-Type': 'multipart/form-data' },
    timeout: 60_000,
  });
  return response.data;
};
