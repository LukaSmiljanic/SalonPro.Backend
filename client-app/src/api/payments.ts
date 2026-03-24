import apiClient from './client';
import type {
  Payment,
  PaymentSummary,
  CreatePaymentRequest,
  UpdatePaymentStatusRequest,
  PaymentStatus,
} from '../types';

// ── Queries ─────────────────────────────────────────────────────────────────

export const getPayments = async (params?: {
  tenantId?: string;
  year?: number;
  month?: number;
  status?: PaymentStatus;
}): Promise<Payment[]> => {
  const { data } = await apiClient.get('/payments', { params });
  return data;
};

export const getPaymentSummary = async (): Promise<PaymentSummary[]> => {
  const { data } = await apiClient.get('/payments/summary');
  return data;
};

// ── Mutations ───────────────────────────────────────────────────────────────

export const createPayment = async (req: CreatePaymentRequest): Promise<string> => {
  const { data } = await apiClient.post('/payments', req);
  return data;
};

export const updatePaymentStatus = async (req: UpdatePaymentStatusRequest): Promise<void> => {
  await apiClient.put(`/payments/${req.id}/status`, req);
};

export const deletePayment = async (id: string): Promise<void> => {
  await apiClient.delete(`/payments/${id}`);
};
