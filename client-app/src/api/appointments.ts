import apiClient from './client';
import type { Appointment, CreateAppointmentRequest, AppointmentListResponse } from '../types';

export const getAppointments = async (params?: {
  date?: string;
  staffId?: string;
  status?: string;
}): Promise<AppointmentListResponse> => {
  const response = await apiClient.get<AppointmentListResponse>('/appointments', { params });
  return response.data;
};

export const createAppointment = async (data: CreateAppointmentRequest): Promise<Appointment> => {
  const response = await apiClient.post<Appointment>('/appointments', data);
  return response.data;
};

export const updateAppointmentStatus = async (id: string, status: string): Promise<Appointment> => {
  const response = await apiClient.patch<Appointment>(`/appointments/${id}/status`, { status });
  return response.data;
};

export const cancelAppointment = async (id: string): Promise<void> => {
  await apiClient.delete(`/appointments/${id}`);
};
