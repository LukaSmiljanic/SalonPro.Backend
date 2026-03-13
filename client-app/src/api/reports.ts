import apiClient from './client';
import type { ReportSummary, StaffRevenue, ServiceRevenue } from '../types';

export const getReportSummary = async (from: string, to: string): Promise<ReportSummary> => {
  const response = await apiClient.get<ReportSummary>('/reports/summary', {
    params: { from, to },
  });
  return response.data;
};

export const getRevenueByStaff = async (from: string, to: string): Promise<StaffRevenue[]> => {
  const response = await apiClient.get<StaffRevenue[]>('/reports/revenue-by-staff', {
    params: { from, to },
  });
  return response.data ?? [];
};

export const getRevenueByService = async (from: string, to: string): Promise<ServiceRevenue[]> => {
  const response = await apiClient.get<ServiceRevenue[]>('/reports/revenue-by-service', {
    params: { from, to },
  });
  return response.data ?? [];
};
