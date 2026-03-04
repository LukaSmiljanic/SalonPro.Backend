import apiClient from './client';
import type { Client, CreateClientRequest, ClientListResponse } from '../types';

export const getClients = async (params?: {
  search?: string;
  page?: number;
  pageSize?: number;
}): Promise<ClientListResponse> => {
  const response = await apiClient.get<ClientListResponse>('/clients', { params });
  return response.data;
};

export const getClient = async (id: string): Promise<Client> => {
  const response = await apiClient.get<Client>(`/clients/${id}`);
  return response.data;
};

export const createClient = async (data: CreateClientRequest): Promise<Client> => {
  const response = await apiClient.post<Client>('/clients', data);
  return response.data;
};

export const updateClient = async (id: string, data: Partial<CreateClientRequest>): Promise<Client> => {
  const response = await apiClient.put<Client>(`/clients/${id}`, data);
  return response.data;
};

export const deleteClient = async (id: string): Promise<void> => {
  await apiClient.delete(`/clients/${id}`);
};
