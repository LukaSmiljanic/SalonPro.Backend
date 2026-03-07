import apiClient from './client';
import type { Client, ClientLoyalty, CreateClientRequest, ClientListResponse } from '../types';

/** Backend GET /clients response (camelCase) */
interface PaginatedClientsResponse {
  items: ClientListDtoItem[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

interface ClientListDtoItem {
  id: string;
  fullName: string;
  phone: string;
  email?: string | null;
  lastVisitDate?: string | null;
  favoriteService?: string | null;
  isVip: boolean;
  tags?: string | null;
}

/** Backend GET /clients/:id response (ClientDetailDto) */
interface ClientDetailDtoResponse {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email?: string | null;
  phone: string;
  notes?: string | null;
  isVip: boolean;
  tags?: string | null;
  totalVisits: number;
  totalSpent: number;
  lastVisitDate?: string | null;
  visitHistory: Array<{ date: string; serviceName: string; staffName: string; price: number }>;
  clientNotes: Array<{ id: string; content: string; createdAt: string; createdBy?: string | null }>;
  loyalty?: {
    totalVisits: number;
    loyaltyTier: string;
    loyaltyBenefit?: string | null;
    nextMilestone?: number | null;
    visitsUntilNextMilestone: number;
    nextMilestoneBenefit?: string | null;
  } | null;
}

function mapListDtoToClient(item: ClientListDtoItem): Client {
  const parts = (item.fullName || '').trim().split(/\s+/);
  const firstName = parts[0] ?? '';
  const lastName = parts.slice(1).join(' ') || '';
  return {
    id: item.id,
    firstName,
    lastName,
    email: item.email ?? undefined,
    phone: item.phone,
    notes: undefined,
    totalVisits: 0,
    totalSpent: 0,
    lastVisit: item.lastVisitDate ?? undefined,
    createdAt: '',
  };
}

function mapDetailDtoToClient(d: ClientDetailDtoResponse): Client {
  const loyalty: ClientLoyalty | undefined = d.loyalty ? {
    totalVisits: d.loyalty.totalVisits,
    loyaltyTier: (d.loyalty.loyaltyTier as ClientLoyalty['loyaltyTier']) || 'None',
    loyaltyBenefit: d.loyalty.loyaltyBenefit ?? undefined,
    nextMilestone: d.loyalty.nextMilestone ?? undefined,
    visitsUntilNextMilestone: d.loyalty.visitsUntilNextMilestone,
    nextMilestoneBenefit: d.loyalty.nextMilestoneBenefit ?? undefined,
  } : undefined;

  return {
    id: d.id,
    firstName: d.firstName,
    lastName: d.lastName,
    email: d.email ?? undefined,
    phone: d.phone,
    notes: d.notes ?? undefined,
    totalVisits: d.totalVisits,
    totalSpent: d.totalSpent,
    lastVisit: d.lastVisitDate ?? undefined,
    createdAt: '',
    loyalty,
  };
}

export const getClients = async (params?: {
  search?: string;
  page?: number;
  pageSize?: number;
}): Promise<ClientListResponse> => {
  const response = await apiClient.get<PaginatedClientsResponse>('/clients', {
    params: {
      pageNumber: params?.page ?? 1,
      pageSize: params?.pageSize ?? 20,
      searchTerm: params?.search ?? undefined,
    },
  });
  const data = response.data;
  return {
    items: (data.items ?? []).map(mapListDtoToClient),
    total: data.totalCount ?? 0,
    page: data.pageNumber ?? 1,
    pageSize: data.pageSize ?? 20,
  };
};

export const getClient = async (id: string): Promise<Client> => {
  const response = await apiClient.get<ClientDetailDtoResponse>(`/clients/${id}`);
  return mapDetailDtoToClient(response.data);
};

export const createClient = async (data: CreateClientRequest): Promise<Client> => {
  const response = await apiClient.post<string>('/clients', data);
  const id = response.data;
  if (!id) throw new Error('Create client did not return id');
  return getClient(String(id));
};

export const updateClient = async (id: string, data: Partial<CreateClientRequest>): Promise<Client> => {
  await apiClient.put(`/clients/${id}`, data);
  return getClient(id);
};

export const deleteClient = async (id: string): Promise<void> => {
  await apiClient.delete(`/clients/${id}`);
};
