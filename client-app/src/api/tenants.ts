import apiClient from './client';
import type { TenantInfo } from '../types';

export const getTenants = async (): Promise<TenantInfo[]> => {
  const { data } = await apiClient.get('/tenants');
  return data;
};
