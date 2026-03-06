import apiClient from './client';
import type { StaffMember, CreateStaffRequest } from '../types';

/** Backend returns fullName, not name */
interface StaffMemberResponse {
  id: string;
  fullName: string;
  specialization?: string | null;
  email?: string | null;
  phone?: string | null;
  isActive: boolean;
  appointmentCountToday?: number;
}

export const getStaff = async (params?: { includeInactive?: boolean }): Promise<StaffMember[]> => {
  const response = await apiClient.get<StaffMemberResponse[]>('/staff', {
    params: { includeInactive: params?.includeInactive ?? false },
  });
  const list = response.data ?? [];
  return list.map(s => ({
    id: s.id,
    name: s.fullName ?? '',
    email: s.email ?? '',
    role: s.specialization ?? '',
    specialties: s.specialization ? [s.specialization] : [],
    isActive: s.isActive,
  }));
};

export const createStaff = async (data: CreateStaffRequest): Promise<string> => {
  const response = await apiClient.post<string>('/staff', {
    firstName: data.firstName.trim(),
    lastName: data.lastName.trim(),
    email: data.email?.trim() || undefined,
    phone: data.phone?.trim() || undefined,
    title: data.title?.trim() || undefined,
    specialization: data.specialization?.trim() || data.title?.trim() || undefined,
    colorIndex: data.colorIndex ?? 0,
  });
  return String(response.data);
};
