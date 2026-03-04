import apiClient from './client';
import type { DashboardStats, RevenueChartPoint } from '../types';

export const getDashboardStats = async (): Promise<DashboardStats> => {
  const response = await apiClient.get<DashboardStats>('/dashboard/stats');
  return response.data;
};

export const getRevenueChart = async (days: number = 30): Promise<RevenueChartPoint[]> => {
  const response = await apiClient.get<RevenueChartPoint[]>(`/dashboard/revenue-chart?days=${days}`);
  return response.data;
};
