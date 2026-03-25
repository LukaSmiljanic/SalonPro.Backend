import apiClient from './client';
import type { DashboardStats, RevenueChartPoint, AppointmentStatus, BirthdayReminder, DashboardInsights, Insight, InsightPriority, InactiveClient } from '../types';

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
  birthdayReminders: Array<{
    clientId: string;
    fullName: string;
    phone?: string;
    email?: string;
    dateOfBirth: string;
    daysUntilBirthday: number;
    age: number;
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
    birthdayReminders: (d.birthdayReminders ?? []).map(b => ({
      clientId: b.clientId,
      fullName: b.fullName,
      phone: b.phone,
      email: b.email,
      dateOfBirth: b.dateOfBirth,
      daysUntilBirthday: b.daysUntilBirthday,
      age: b.age,
    })),
  };
};

export const getBirthdayReminders = async (days: number = 7): Promise<BirthdayReminder[]> => {
  const response = await apiClient.get<BirthdayReminder[]>(`/dashboard/birthday-reminders?days=${days}`);
  return response.data ?? [];
};

export const getRevenueChart = async (days: number = 30): Promise<RevenueChartPoint[]> => {
  const response = await apiClient.get<RevenueChartResponse>(`/dashboard/revenue-chart?days=${days}`);
  const points = response.data?.dataPoints ?? [];
  return points.map(p => ({
    date: typeof p.date === 'string' ? p.date.slice(0, 10) : p.date,
    revenue: Number(p.value),
  }));
};

/** Backend GET /dashboard/insights response */
interface DashboardInsightsResponse {
  insights: Array<{
    type: string;
    priority: string;
    title: string;
    description: string;
    icon: string;
    actionLabel?: string;
    actionData?: string;
  }>;
  inactiveClientsCount: number;
  scheduleGapsCount: number;
  weekRevenueChangePercent: number;
  inactiveClients: Array<{
    id: string;
    fullName: string;
    lastVisit?: string;
  }>;
}

function mapPriority(p: string): InsightPriority {
  if (p === 'Urgent') return 'Urgent';
  if (p === 'High') return 'High';
  if (p === 'Medium') return 'Medium';
  return 'Low';
}

export const getDashboardInsights = async (): Promise<DashboardInsights> => {
  const response = await apiClient.get<DashboardInsightsResponse>('/dashboard/insights');
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
    inactiveClientsCount: d.inactiveClientsCount ?? 0,
    scheduleGapsCount: d.scheduleGapsCount ?? 0,
    weekRevenueChangePercent: d.weekRevenueChangePercent ?? 0,
    inactiveClients: (d.inactiveClients ?? []).map(c => ({
      id: c.id,
      fullName: c.fullName,
      lastVisit: c.lastVisit,
    })),
  };
};
