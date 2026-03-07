import React, { useState } from 'react';
import {
  AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer
} from 'recharts';
import { useQuery } from '@tanstack/react-query';
import { format, parseISO, isToday, isTomorrow } from 'date-fns';
import { srLatn } from 'date-fns/locale';
import {
  TrendingUp, Users, Calendar, CheckCircle2, Plus, RefreshCw, Cake, Gift, Phone
} from 'lucide-react';
import { getDashboardStats, getRevenueChart } from '../api/dashboard';
import type { DashboardStats, RevenueChartPoint } from '../types';
import { queryKeys } from '../lib/queryKeys';
import { KpiCard } from '../components/KpiCard';
import { Badge } from '../components/Badge';
import { Button } from '../components/Button';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { EmptyState } from '../components/EmptyState';
import { CreateAppointmentModal } from '../components/CreateAppointmentModal';
import { AiInsightsPanel } from '../components/AiInsightsPanel';

const REFRESH_INTERVAL_MS = 60_000;

export const DashboardPage: React.FC = () => {
  const [createAppointmentModalOpen, setCreateAppointmentModalOpen] = useState(false);

  const { data: stats, isLoading: statsLoading, error: statsError, refetch: refetchStats, dataUpdatedAt } = useQuery({
    queryKey: queryKeys.dashboard.stats(),
    queryFn: getDashboardStats,
    refetchInterval: REFRESH_INTERVAL_MS,
  });

  const { data: chartData = [], isLoading: chartLoading } = useQuery({
    queryKey: queryKeys.dashboard.revenueChart(30),
    queryFn: () => getRevenueChart(30),
    refetchInterval: REFRESH_INTERVAL_MS,
  });

  const isLoading = statsLoading || chartLoading;
  const error = statsError ? 'Nije moguće učitati podatke. Pokušajte ponovo.' : null;
  const lastUpdated = dataUpdatedAt ? new Date(dataUpdatedAt) : null;

  const formatDateLabel = (dateStr: string) => {
    const date = parseISO(dateStr);
    if (isToday(date)) return 'Danas';
    if (isTomorrow(date)) return 'Sutra';
    return format(date, 'EEE, MMM d', { locale: srLatn });
  };

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('sr-Latn-RS', {
      style: 'currency',
      currency: 'RSD',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(value);
  };

  const formatChartDate = (dateStr: string) => {
    try {
      return format(parseISO(dateStr), 'MMM d', { locale: srLatn });
    } catch {
      return dateStr;
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-[calc(100vh-56px)]">
        <LoadingSpinner size="lg" />
      </div>
    );
  }

  return (
    <div className="container-main py-6 space-y-6">

      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold text-display text-text">Kontrolna tabla</h1>
          {lastUpdated && (
            <p className="text-xs text-text-faint mt-0.5">
              Ažurirano {format(lastUpdated, 'HH:mm')}
            </p>
          )}
        </div>
        <div className="flex items-center gap-2">
          <Button variant="secondary" size="sm" icon={<RefreshCw size={13} />} onClick={() => refetchStats()}>
            Osveži
          </Button>
          <Button size="sm" icon={<Plus size={13} />} onClick={() => setCreateAppointmentModalOpen(true)}>
            Novi termin
          </Button>
        </div>
      </div>

      {/* Error banner */}
      {error && (
        <div className="p-3 bg-error-bg border border-error/20 rounded-lg text-sm text-error">
          {error}
        </div>
      )}

      {/* KPI Cards */}
      {stats && (
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <KpiCard
            label="Termini danas"
            value={stats.todayAppointments}
            icon={<Calendar size={16} />}
            subtext="Zakazano danas"
          />
          <KpiCard
            label="Prihod nedelja"
            value={formatCurrency(stats.weekRevenue)}
            icon={<TrendingUp size={16} />}
            subtext="Poslednjih 7 dana"
          />
          <KpiCard
            label="Aktivni klijenti"
            value={stats.activeClients}
            icon={<Users size={16} />}
            subtext="Ukupno registrovanih"
          />
          <KpiCard
            label="Stopa završenosti"
            value={`${stats?.completionRate?.toFixed(1)}%`}
            icon={<CheckCircle2 size={16} />}
            subtext="Ovog meseca"
          />
        </div>
      )}

      {/* Birthday Reminders Widget */}
      {stats?.birthdayReminders && stats.birthdayReminders.length > 0 && (
        <div className="card card-padded">
          <div className="flex items-center gap-2 mb-4">
            <div className="w-8 h-8 rounded-lg bg-[#5B3A8C]/10 flex items-center justify-center">
              <Cake size={16} className="text-[#5B3A8C]" />
            </div>
            <div>
              <h2 className="text-sm font-semibold text-text">Rođendani ove nedelje</h2>
              <p className="text-xs text-text-faint">{stats.birthdayReminders.length} klijent{stats.birthdayReminders.length === 1 ? '' : 'a'}</p>
            </div>
          </div>
          <div className="space-y-2">
            {stats.birthdayReminders.map(reminder => (
              <div
                key={reminder.clientId}
                className="flex items-center gap-3 p-3 bg-surface-2 rounded-lg"
              >
                <div className="w-9 h-9 rounded-full bg-[#5B3A8C]/10 flex items-center justify-center shrink-0">
                  <Gift size={15} className="text-[#5B3A8C]" />
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-text truncate">{reminder.fullName}</p>
                  <div className="flex items-center gap-2">
                    <span className="text-xs text-text-muted">
                      {reminder.daysUntilBirthday === 0
                        ? '🎂 Danas!'
                        : reminder.daysUntilBirthday === 1
                        ? 'Sutra'
                        : `Za ${reminder.daysUntilBirthday} dana`}
                    </span>
                    <span className="text-xs text-text-faint">· Puni {reminder.age} god.</span>
                  </div>
                </div>
                {reminder.phone && (
                  <a
                    href={`tel:${reminder.phone}`}
                    className="p-1.5 rounded-md text-text-faint hover:text-[#5B3A8C] hover:bg-[#5B3A8C]/10 transition-interactive"
                    title="Pozovi"
                  >
                    <Phone size={14} />
                  </a>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Main content grid */}
      <div className="grid lg:grid-cols-3 gap-6">

        {/* Revenue chart */}
        <div className="lg:col-span-2 card card-padded">
          <h2 className="text-sm font-semibold text-text mb-4">Prihod (poslednjih 30 dana)</h2>
          {chartData.length > 0 ? (
            <ResponsiveContainer width="100%" height={220}>
              <AreaChart data={chartData} margin={{ top: 4, right: 4, bottom: 0, left: -10 }}>
                <defs>
                  <linearGradient id="revenueGrad" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#5B3A8C" stopOpacity={0.15} />
                    <stop offset="95%" stopColor="#5B3A8C" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--color-divider)" vertical={false} />
                <XAxis
                  dataKey="date"
                  tickFormatter={formatChartDate}
                  tick={{ fontSize: 11, fill: 'var(--color-text-faint)' }}
                  axisLine={false}
                  tickLine={false}
                  interval="preserveStartEnd"
                />
                <YAxis
                  tickFormatter={v => `${formatCurrency(v)}`}
                  tick={{ fontSize: 11, fill: 'var(--color-text-faint)' }}
                  axisLine={false}
                  tickLine={false}
                />
                <Tooltip
                  formatter={(v: number) => [formatCurrency(v), 'Prihod']}
                  labelFormatter={formatChartDate}
                  contentStyle={{
                    background: 'var(--color-surface)',
                    border: '1px solid var(--color-border)',
                    borderRadius: 'var(--radius-md)',
                    fontSize: 12,
                  }}
                />
                <Area
                  type="monotone"
                  dataKey="revenue"
                  stroke="#5B3A8C"
                  strokeWidth={2}
                  fill="url(#revenueGrad)"
                  dot={false}
                  activeDot={{ r: 4, fill: '#5B3A8C' }}
                />
              </AreaChart>
            </ResponsiveContainer>
          ) : (
            <EmptyState
              title="Nema podataka o prihodu"
              description="Prihod će se pojaviti kada budete imali završene termine."
            />
          )}
        </div>

        {/* Upcoming appointments */}
        <div className="card card-padded">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-sm font-semibold text-text">Predstojeći</h2>
            <span className="text-xs text-text-faint">
              {stats?.upcomingAppointments?.length ?? 0} termina
            </span>
          </div>

          {stats?.upcomingAppointments?.length ? (
            <div className="space-y-3">
              {stats.upcomingAppointments.map(appt => (
                <div key={appt.id} className="flex flex-col gap-1 p-3 bg-surface-2 rounded-lg">
                  <div className="flex items-center justify-between">
                    <span className="text-sm font-medium text-text truncate">{appt.clientName}</span>
                    <Badge status={appt.status} />
                  </div>
                  <span className="text-xs text-text-muted">{appt.serviceName}</span>
                  <div className="flex items-center justify-between mt-1">
                    <span className="text-xs text-text-faint">{appt.staffName}</span>
                    <span className="text-xs text-text-faint">
                      {formatDateLabel(appt.startTime)}, {format(parseISO(appt.startTime), 'HH:mm')}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <EmptyState
              title="Nema predstojećih termina"
              description="Zakažite novi termin da biste počeli."
              action={
                <Button size="sm" icon={<Plus size={12} />} onClick={() => setCreateAppointmentModalOpen(true)}>
                  Novi termin
                </Button>
              }
            />
          )}
        </div>
      </div>

      {/* AI Insights Panel */}
      <AiInsightsPanel />

      <CreateAppointmentModal
        isOpen={createAppointmentModalOpen}
        onClose={() => setCreateAppointmentModalOpen(false)}
      />
    </div>
  );
};
