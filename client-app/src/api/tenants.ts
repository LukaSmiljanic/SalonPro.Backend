import apiClient from './client';
import type { TenantInfo } from '../types';

export const getTenants = async (): Promise<TenantInfo[]> => {
  const { data } = await apiClient.get('/tenants');
  return data;
};

export interface ExtendSubscriptionRequest {
  tenantId: string;
  days: number;
}

export interface ExtendSubscriptionResponse {
  message: string;
  newEndDate: string;
}

export const extendSubscription = async (req: ExtendSubscriptionRequest): Promise<ExtendSubscriptionResponse> => {
  const { data } = await apiClient.post('/subscriptions/extend', req);
  return data;
};

export interface SubscriptionStatus {
  tenantId: string;
  tenantName: string;
  emailVerified: boolean;
  isTrialing: boolean;
  status: string;
  subscriptionStartDate?: string;
  subscriptionEndDate?: string;
  daysRemaining?: number;
}

export const getSubscriptionStatus = async (tenantId: string): Promise<SubscriptionStatus> => {
  const { data } = await apiClient.get(`/subscriptions/${tenantId}/status`);
  return data;
};
