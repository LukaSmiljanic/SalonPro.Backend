import apiClient from './client';
import type { StaffMember, CreateStaffRequest } from '../types';

/** Backend returns individual fields now */
interface StaffMemberResponse {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  specialization?: string | null;
  email?: string | null;
  phone?: string | null;
  isActive: boolean;
  colorIndex: number;
  appointmentCountToday?: number;
  totalAppointments?: number;
}

function mapStaff(s: StaffMemberResponse): StaffMember {
  return {
    id: s.id,
    firstName: s.firstName ?? '',
    lastName: s.lastName ?? '',
    name: s.fullName ?? '',
    email: s.email ?? '',
    phone: s.phone ?? '',
    role: s.specialization ?? '',
    specialties: s.specialization ? [s.specialization] : [],
    isActive: s.isActive,
    colorIndex: s.colorIndex ?? 0,
    totalAppointments: s.totalAppointments ?? 0,
  };
}

export const getStaff = async (params?: { includeInactive?: boolean }): Promise<StaffMember[]> => {
  const response = await apiClient.get<StaffMemberResponse[]>('/staff', {
    params: { includeInactive: params?.includeInactive ?? false },
  });
  return (response.data ?? []).map(mapStaff);
};

export const createStaff = async (data: CreateStaffRequest): Promise<string> => {
  const response = await apiClient.post<string>('/staff', {
    firstName: data.firstName.trim(),
    lastName: data.lastName.trim(),
    email: data.email?.trim() || undefined,
    phone: data.phone?.trim() || undefined,
    title: data.specialization?.trim() || data.title?.trim() || undefined,
    specialization: data.specialization?.trim() || data.title?.trim() || undefined,
    colorIndex: data.colorIndex ?? 0,
  });
  return String(response.data);
};

export interface UpdateStaffRequest {
  firstName: string;
  lastName: string;
  email?: string;
  phone?: string;
  specialization?: string;
  isActive: boolean;
  colorIndex: number;
}

export const updateStaff = async (id: string, data: UpdateStaffRequest): Promise<void> => {
  await apiClient.put(`/staff/${id}`, {
    id,
    firstName: data.firstName.trim(),
    lastName: data.lastName.trim(),
    email: data.email?.trim() || null,
    phone: data.phone?.trim() || null,
    specialization: data.specialization?.trim() || null,
    isActive: data.isActive,
    colorIndex: data.colorIndex,
  });
};

export interface DeleteStaffResult {
  wasSoftDeleted: boolean;
  message: string;
}

export const deleteStaff = async (id: string): Promise<DeleteStaffResult> => {
  const response = await apiClient.delete<DeleteStaffResult>(`/staff/${id}`);
  return response.data;
};
