import apiClient from './client';
import type { Appointment, CreateAppointmentRequest, AppointmentListResponse } from '../types';

/** Response shape from GET /appointments/by-date (array, camelCase from backend) */
interface AppointmentByDateItem {
  id: string;
  clientName: string;
  staffMemberName: string;
  startTime: string;
  endTime: string;
  status: string;
  totalPrice: number;
  notes?: string | null;
  services?: Array<{ serviceId: string; serviceName: string; price: number; durationMinutes: number }>;
}

/** Response shape from GET /appointments/{id} (detail, includes clientId/staffMemberId) */
interface AppointmentDetailItem {
  id: string;
  clientId: string;
  clientName: string;
  staffMemberId: string;
  staffMemberName: string;
  startTime: string;
  endTime: string;
  status: string;
  totalPrice: number;
  notes?: string | null;
  cancellationReason?: string | null;
  services?: Array<{ serviceId: string; serviceName: string; price: number; durationMinutes: number }>;
}

function mapAppointmentFromApi(item: AppointmentByDateItem): Appointment {
  const services = item.services ?? [];
  const firstService = services[0];
  const statusMap: Record<string, Appointment['status']> = {
    Scheduled: 'Confirmed',
    Pending: 'Pending',
    Confirmed: 'Confirmed',
    InProgress: 'InProgress',
    Completed: 'Completed',
    Cancelled: 'Cancelled',
    NoShow: 'NoShow',
  };
  return {
    id: item.id,
    clientId: '',
    clientName: item.clientName,
    staffId: '',
    staffName: item.staffMemberName,
    serviceId: firstService?.serviceId ?? '',
    serviceName: services.map(s => s.serviceName).join(', ') || '—',
    serviceCategory: '',
    startTime: item.startTime,
    endTime: item.endTime,
    status: statusMap[item.status] ?? 'Pending',
    notes: item.notes ?? undefined,
    price: item.totalPrice,
  };
}

/**
 * Fetches appointments from the backend.
 * - When date is "YYYY-MM-DD/YYYY-MM-DD" (range): calls by-date for each day and merges.
 * - When date is "YYYY-MM-DD": single by-date call.
 * - staffId: optional, maps to staffMemberId query param.
 */
export const getAppointments = async (params?: {
  date?: string;
  staffId?: string;
  status?: string;
}): Promise<AppointmentListResponse> => {
  const dateParam = params?.date;
  const staffMemberId = params?.staffId && params.staffId !== 'all' ? params.staffId : undefined;

  if (!dateParam) {
    const today = new Date().toISOString().slice(0, 10);
    const response = await apiClient.get<AppointmentByDateItem[]>(
      '/appointments/by-date',
      { params: { date: today, staffMemberId } }
    );
    const items = (response.data ?? []).map(mapAppointmentFromApi);
    return { items, total: items.length, page: 1, pageSize: items.length };
  }

  const isRange = dateParam.includes('/');
  if (isRange) {
    const [startStr, endStr] = dateParam.split('/');
    if (!startStr || !endStr) {
      return { items: [], total: 0, page: 1, pageSize: 0 };
    }
    const all: Appointment[] = [];
    const seen = new Set<string>();
    const startDate = new Date(startStr + 'T12:00:00');
    const endDate = new Date(endStr + 'T12:00:00');
    for (let d = new Date(startDate); d <= endDate; d.setDate(d.getDate() + 1)) {
      const dateStr = d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0');
      const response = await apiClient.get<AppointmentByDateItem[]>(
        '/appointments/by-date',
        { params: { date: dateStr, staffMemberId } }
      );
      const list = response.data ?? [];
      for (const item of list) {
        if (!seen.has(item.id)) {
          seen.add(item.id);
          all.push(mapAppointmentFromApi(item));
        }
      }
    }
    return { items: all, total: all.length, page: 1, pageSize: all.length };
  }

  const response = await apiClient.get<AppointmentByDateItem[]>(
    '/appointments/by-date',
    { params: { date: dateParam, staffMemberId } }
  );
  const items = (response.data ?? []).map(mapAppointmentFromApi);
  return { items, total: items.length, page: 1, pageSize: items.length };
};

export const createAppointment = async (data: CreateAppointmentRequest): Promise<string> => {
  const body = {
    clientId: data.clientId,
    staffMemberId: data.staffId,
    startTime: data.startTime,
    serviceIds: [data.serviceId],
    notes: data.notes ?? null,
  };
  const response = await apiClient.post<string>('/appointments', body);
  return response.data;
};

export const updateAppointmentStatus = async (id: string, status: string): Promise<Appointment> => {
  const response = await apiClient.patch<Appointment>(`/appointments/${id}/status`, { status });
  return response.data;
};

export const cancelAppointment = async (id: string, cancellationReason?: string): Promise<void> => {
  await apiClient.patch(`/appointments/${id}/cancel`, { id, cancellationReason: cancellationReason ?? null });
};

export const completeAppointment = async (id: string): Promise<void> => {
  await apiClient.patch(`/appointments/${id}/complete`);
};

/** Fetch full appointment detail (includes clientId, staffMemberId) */
export const getAppointmentDetail = async (id: string): Promise<AppointmentDetailItem> => {
  const response = await apiClient.get<AppointmentDetailItem>(`/appointments/${id}`);
  return response.data;
};

/** Reschedule an appointment to a new start time.
 *  First fetches full detail to get clientId/staffMemberId,
 *  then calls PUT /appointments/{id}.
 */
export const rescheduleAppointment = async (
  id: string,
  newStartTime: string,
): Promise<void> => {
  // Fetch full detail to get the IDs we need
  const detail = await getAppointmentDetail(id);

  const serviceIds = detail.services?.map(s => s.serviceId) ?? [];

  await apiClient.put(`/appointments/${id}`, {
    id,
    clientId: detail.clientId,
    staffMemberId: detail.staffMemberId,
    startTime: newStartTime,
    serviceIds: serviceIds.length > 0 ? serviceIds : [],
    notes: detail.notes ?? null,
    status: detail.status,
  });
};
