import apiClient from './client';
import type { Service, ServiceCategory, CreateServiceCategoryRequest, CreateServiceRequest } from '../types';

/** Backend GET /services response item (camelCase) */
interface ServiceDtoItem {
  id: string;
  categoryId: string;
  categoryName: string;
  name: string;
  description?: string | null;
  durationMinutes: number;
  price: number;
  isActive: boolean;
}

export const getServices = async (params?: { includeInactive?: boolean }): Promise<Service[]> => {
  const response = await apiClient.get<ServiceDtoItem[]>(
    '/services',
    { params: { includeInactive: params?.includeInactive ?? false } }
  );
  return (response.data ?? []).map(s => ({
    id: s.id,
    name: s.name,
    category: s.categoryName ?? '',
    categoryId: s.categoryId,
    duration: s.durationMinutes,
    price: Number(s.price),
    description: s.description ?? undefined,
    isActive: s.isActive,
  }));
};

/** Backend GET /service-categories response (camelCase) */
interface ServiceCategoryDtoItem {
  id: string;
  name: string;
  description?: string | null;
  colorHex?: string | null;
  type: string;
  isActive: boolean;
  serviceCount: number;
}

export const getCategories = async (params?: { includeInactive?: boolean }): Promise<ServiceCategory[]> => {
  const response = await apiClient.get<ServiceCategoryDtoItem[]>('/service-categories', {
    params: { includeInactive: params?.includeInactive ?? false },
  });
  return (response.data ?? []).map(c => ({
    id: c.id,
    name: c.name,
    description: c.description ?? undefined,
    colorHex: c.colorHex ?? undefined,
    type: (c.type as ServiceCategory['type']) ?? 'Other',
    isActive: c.isActive,
    serviceCount: c.serviceCount ?? 0,
  }));
};

export const createCategory = async (data: CreateServiceCategoryRequest): Promise<string> => {
  const response = await apiClient.post<string>('/service-categories', {
    name: data.name,
    description: data.description ?? undefined,
    colorHex: data.colorHex ?? undefined,
    type: data.type ?? 'Other',
  });
  return String(response.data);
};

export const createService = async (data: CreateServiceRequest): Promise<string> => {
  const response = await apiClient.post<string>('/services', {
    categoryId: data.categoryId,
    name: data.name,
    description: data.description ?? undefined,
    durationMinutes: data.durationMinutes,
    price: data.price,
  });
  return String(response.data);
};

export interface UpdateServiceRequest {
  categoryId: string;
  name: string;
  description?: string;
  durationMinutes: number;
  price: number;
  isActive: boolean;
}

export const updateService = async (id: string, data: UpdateServiceRequest): Promise<void> => {
  await apiClient.put(`/services/${id}`, {
    id,
    categoryId: data.categoryId,
    name: data.name,
    description: data.description ?? undefined,
    durationMinutes: data.durationMinutes,
    price: data.price,
    isActive: data.isActive,
  });
};

export const deleteService = async (id: string): Promise<void> => {
  await apiClient.delete(`/services/${id}`);
};
