import apiClient from './client';
import type { DashboardStats, RevenueChartPoint, AppointmentStatus } from '../types';

/** Backend GET /dashboard/stats response (camelCase) */
interface DashboardStatsResponse {
  todayRevenue: number;
  revenueChangePercent: number;
  appointmentsToday: number;
  appointmentsPending: number;
  newClientsThisMonth: number;
  newClientsChangePercent: number;
  occupancyRatePercent: number;
  occupancyChangePercent: number;
  weekRevenue: number;
  totalClients: number;
  completionRate: number;
  upcomingAppointments: Array<{
    id: string;
    clientName: string;
    serviceName: string;
    staffName: string;
    startTime: string;
    status: string;
  }>;
}

/** Backend GET /dashboard/revenue-chart response */
interface RevenueChartResponse {
  dataPoints: Array<{ label: string; value: number; date: string }>;
}

function mapStatus(backend: string): AppointmentStatus {
  if (backend === 'Scheduled') return 'Pending';
  if (backend === 'Confirmed') return 'Confirmed';
  if (backend === 'InProgress') return 'InProgress';
  if (backend === 'Completed') return 'Completed';
  if (backend === 'Cancelled') return 'Cancelled';
  if (backend === 'NoShow') return 'NoShow';
  return 'Pending';
}

export const getDashboardStats = async (): Promise<DashboardStats> => {
  const response = await apiClient.get<DashboardStatsResponse>('/dashboard/stats');
  const d = response.data;
  return {
    todayAppointments: d.appointmentsToday ?? 0,
    weekRevenue: d.weekRevenue ?? 0,
    activeClients: d.totalClients ?? 0,
    completionRate: d.completionRate ?? 0,
    upcomingAppointments: (d.upcomingAppointments ?? []).map(a => ({
      id: a.id,
      clientName: a.clientName,
      serviceName: a.serviceName,
      staffName: a.staffName,
      startTime: a.startTime,
      status: mapStatus(a.status),
    })),
  };
};

export const getRevenueChart = async (days: number = 30): Promise<RevenueChartPoint[]> => {
  const response = await apiClient.get<RevenueChartResponse>(`/dashboard/revenue-chart?days=${days}`);
  const points = response.data?.dataPoints ?? [];
  return points.map(p => ({
    date: typeof p.date === 'string' ? p.date.slice(0, 10) : p.date,
    revenue: Number(p.value),
  }));
};
