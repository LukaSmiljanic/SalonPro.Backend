/**
 * Central query keys for React Query.
 * Use these when invalidating after mutations so all related views update.
 */
export const queryKeys = {
  dashboard: {
    all: ['dashboard'] as const,
    stats: () => [...queryKeys.dashboard.all, 'stats'] as const,
    revenueChart: (days: number) => [...queryKeys.dashboard.all, 'revenue', days] as const,
    insights: () => [...queryKeys.dashboard.all, 'insights'] as const,
  },
  appointments: {
    all: ['appointments'] as const,
    byDate: (date: string, staffId?: string) =>
      [...queryKeys.appointments.all, date, staffId ?? 'all'] as const,
  },
  clients: {
    all: ['clients'] as const,
    list: (params: { search?: string; page: number; pageSize: number }) =>
      [...queryKeys.clients.all, 'list', params] as const,
    detail: (id: string) => [...queryKeys.clients.all, 'detail', id] as const,
    insights: (id: string) => [...queryKeys.clients.all, 'insights', id] as const,
  },
  services: {
    all: ['services'] as const,
    list: (includeInactive?: boolean) =>
      [...queryKeys.services.all, 'list', includeInactive ?? false] as const,
  },
  serviceCategories: {
    all: ['serviceCategories'] as const,
    list: (includeInactive?: boolean) =>
      [...queryKeys.serviceCategories.all, 'list', includeInactive ?? false] as const,
  },
  staff: {
    all: ['staff'] as const,
    list: () => [...queryKeys.staff.all, 'list'] as const,
  },
} as const;
