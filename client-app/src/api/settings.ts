import apiClient from './client';
import type { WorkingHoursEntry, LoyaltyTierConfig } from '../types';

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
