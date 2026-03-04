import React, { useState, useEffect, useCallback } from 'react';
import {
  AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer
} from 'recharts';
import { format, parseISO, isToday, isTomorrow } from 'date-fns';
import {
  TrendingUp, Users, Calendar, CheckCircle2, Plus, RefreshCw
} from 'lucide-react';
import { getDashboardStats, getRevenueChart } from '../api/dashboard';
import type { DashboardStats, RevenueChartPoint } from '../types';
import { KpiCard } from '../components/KpiCard';
import { Badge } from '../components/Badge';
import { Button } from '../components/Button';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { EmptyState } from '../components/EmptyState';

const REFRESH_INTERVAL = 60_000; // 1 min

export const DashboardPage: React.FC = () => {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [chartData, setChartData] = useState<RevenueChartPoint[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

  const fetchData = useCallback(async () => {
    try {
      setError(null);
      const [statsData, chartPoints] = await Promise.all([
        getDashboardStats(),
        getRevenueChart(30),
      ]);
      setStats(statsData);
      setChartData(chartPoints);
      setLastUpdated(new Date());
    } catch (err) {
      console.error('Dashboard fetch error:', err);
      setError('Failed to load dashboard data. Please try again.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
    const interval = setInterval(fetchData, REFRESH_INTERVAL);
    return () => clearInterval(interval);
  }, [fetchData]);

  const formatDateLabel = (dateStr: string) => {
    const date = parseISO(dateStr);
    if (isToday(date)) return 'Today';
    if (isTomorrow(date)) return 'Tomorrow';
    return format(date, 'EEE, MMM d');
  };

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'EUR',
      minimumFractionDigits: 0,
    }).format(value);
  };

  const formatChartDate = (dateStr: string) => {
    try {
      return format(parseISO(dateStr), 'MMM d');
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
          <h1 className="text-xl font-semibold text-display text-text">Dashboard</h1>
          {lastUpdated && (
            <p className="text-xs text-text-faint mt-0.5">
              Updated {format(lastUpdated, 'HH:mm')}
            </p>
          )}
        </div>
        <div className="flex items-center gap-2">
          <Button variant="secondary" size="sm" icon={<RefreshCw size={13} />} onClick={fetchData}>
            Refresh
          </Button>
          <Button size="sm" icon={<Plus size={13} />}>
            New Appointment
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
            label="Today's Appointments"
            value={stats.todayAppointments}
            icon={<Calendar size={16} />}
            subtext="Scheduled today"
          />
          <KpiCard
            label="Week Revenue"
            value={formatCurrency(stats.weekRevenue)}
            icon={<TrendingUp size={16} />}
            subtext="Last 7 days"
          />
          <KpiCard
            label="Active Clients"
            value={stats.activeClients}
            icon={<Users size={16} />}
            subtext="Total registered"
          />
          <KpiCard
            label="Completion Rate"
            value={`${stats.completionRate.toFixed(1)}%`}
            icon={<CheckCircle2 size={16} />}
            subtext="This month"
          />
        </div>
      )}

      {/* Main content grid */}
      <div className="grid lg:grid-cols-3 gap-6">

        {/* Revenue chart */}
        <div className="lg:col-span-2 card card-padded">
          <h2 className="text-sm font-semibold text-text mb-4">Revenue (Last 30 Days)</h2>
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
                  tickFormatter={v => `€${v}`}
                  tick={{ fontSize: 11, fill: 'var(--color-text-faint)' }}
                  axisLine={false}
                  tickLine={false}
                />
                <Tooltip
                  formatter={(v: number) => [formatCurrency(v), 'Revenue']}
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
              title="No revenue data"
              description="Revenue will appear here once you have completed appointments."
            />
          )}
        </div>

        {/* Upcoming appointments */}
        <div className="card card-padded">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-sm font-semibold text-text">Upcoming</h2>
            <span className="text-xs text-text-faint">
              {stats?.upcomingAppointments.length ?? 0} appointments
            </span>
          </div>

          {stats?.upcomingAppointments.length ? (
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
              title="No upcoming appointments"
              description="Schedule a new appointment to get started."
              action={
                <Button size="sm" icon={<Plus size={12} />}>New Appointment</Button>
              }
            />
          )}
        </div>
      </div>
    </div>
  );
};
