import apiClient from './client';
import type { Client, ClientLoyalty, CreateClientRequest, ClientListResponse, ClientInsights, Insight, InsightPriority } from '../types';

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
  totalVisits: number;
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
    isVip: item.isVip,
    tags: item.tags ?? undefined,
    totalVisits: item.totalVisits ?? 0,
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
    isVip: d.isVip,
    tags: d.tags ?? undefined,
    totalVisits: d.totalVisits,
    totalSpent: d.totalSpent,
    lastVisit: d.lastVisitDate ?? undefined,
    createdAt: '',
    loyalty,
    visitHistory: (d.visitHistory ?? []).map(v => ({
      date: v.date,
      serviceName: v.serviceName,
      staffName: v.staffName,
      price: v.price,
    })),
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

export const updateClient = async (id: string, data: Partial<CreateClientRequest> & { isVip?: boolean; tags?: string }): Promise<Client> => {
  // Backend UpdateClientCommand requires all fields including Id in body
  const body = {
    id,
    firstName: data.firstName ?? '',
    lastName: data.lastName ?? '',
    email: data.email || null,
    phone: data.phone ?? '',
    notes: data.notes || null,
    isVip: data.isVip ?? false,
    tags: data.tags || null,
  };
  await apiClient.put(`/clients/${id}`, body);
  return getClient(id);
};

export const deleteClient = async (id: string): Promise<void> => {
  await apiClient.delete(`/clients/${id}`);
};

/** Backend GET /clients/:id/insights response */
interface ClientInsightsResponse {
  insights: Array<{
    type: string;
    priority: string;
    title: string;
    description: string;
    icon: string;
    actionLabel?: string;
    actionData?: string;
  }>;
  averageVisitCycleDays: number;
  suggestedNextVisit?: string | null;
  preferredStaffName?: string | null;
  topService?: string | null;
  averageSpendPerVisit: number;
}

function mapPriority(p: string): InsightPriority {
  if (p === 'Urgent') return 'Urgent';
  if (p === 'High') return 'High';
  if (p === 'Medium') return 'Medium';
  return 'Low';
}

export const getClientInsights = async (id: string): Promise<ClientInsights> => {
  const response = await apiClient.get<ClientInsightsResponse>(`/clients/${id}/insights`);
  const d = response.data;
  return {
    insights: (d.insights ?? []).map(i => ({
      type: i.type as Insight['type'],
      priority: mapPriority(i.priority),
      title: i.title,
      description: i.description,
      icon: i.icon,
      actionLabel: i.actionLabel,
      actionData: i.actionData,
    })),
    averageVisitCycleDays: d.averageVisitCycleDays ?? 0,
    suggestedNextVisit: d.suggestedNextVisit ?? undefined,
    preferredStaffName: d.preferredStaffName ?? undefined,
    topService: d.topService ?? undefined,
    averageSpendPerVisit: d.averageSpendPerVisit ?? 0,
  };
};
