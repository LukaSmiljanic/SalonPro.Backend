import apiClient from './client';
import type { StaffMember } from '../types';

export const getStaff = async (): Promise<StaffMember[]> => {
  const response = await apiClient.get<StaffMember[]>('/staff');
  return response.data;
};
