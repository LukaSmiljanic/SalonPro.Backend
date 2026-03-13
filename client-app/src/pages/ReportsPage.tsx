import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { format, subDays } from 'date-fns';
import {
  TrendingUp, Calendar, Users, XCircle, RefreshCw
} from 'lucide-react';
import { getReportSummary, getRevenueByStaff, getRevenueByService } from '../api/reports';
import type { StaffRevenue, ServiceRevenue } from '../types';
import { queryKeys } from '../lib/queryKeys';
import { KpiCard } from '../components/KpiCard';
import { Button } from '../components/Button';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { EmptyState } from '../components/EmptyState';

// ── Helpers ────────────────────────────────────────────────────────────────────────────

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('sr-Latn-RS', {
    style: 'currency',
    currency: 'RSD',
    minimumFractionDigits: 0,
  }).format(value);

const today = () => format(new Date(), 'yyyy-MM-dd');
const daysAgo = (n: number) => format(subDays(new Date(), n), 'yyyy-MM-dd');

// ── Staff Revenue Table ──────────────────────────────────────────────────────────────────────────

interface StaffTableProps {
  data: StaffRevenue[];
}

const StaffRevenueTable: React.FC<StaffTableProps> = ({ data }) => {
  if (data.length === 0) {
    return (
      <EmptyState
        title="Nema podataka"
        description="Za izabrani period nema prihoda po zaposlenom."
      />
    );
  }

  const maxRevenue = Math.max(...data.map(s => s.totalRevenue), 1);

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-divider">
            <th className="text-left py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide">
              Zaposleni
            </th>
            <th className="text-right py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide">
              Prihod
            </th>
            <th className="text-right py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide">
              Termina
            </th>
            <th className="text-right py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide">
              Prosek
            </th>
          </tr>
        </thead>
        <tbody>
          {data.map(row => {
            const barWidth = maxRevenue > 0 ? (row.totalRevenue / maxRevenue) * 100 : 0;
            return (
              <tr
                key={row.staffId}
                className="border-b border-divider last:border-0 hover:bg-surface-2 transition-colors duration-150"
              >
                <td className="py-3 px-3 font-medium text-text">
                  {row.staffName}
                </td>
                <td className="py-3 px-3 text-right">
                  <div className="flex items-center justify-end gap-2">
                    {/* Bar visual */}
                    <div className="hidden sm:block w-20 h-1.5 bg-surface-offset rounded-full overflow-hidden">
                      <div
                        className="h-full bg-primary rounded-full transition-all duration-500"
                        style={{ width: `${barWidth}%` }}
                      />
                    </div>
                    <span className="font-semibold text-text tabular-nums">
                      {formatCurrency(row.totalRevenue)}
                    </span>
                  </div>
                </td>
                <td className="py-3 px-3 text-right text-text-muted tabular-nums">
                  {row.appointmentCount}
                </td>
                <td className="py-3 px-3 text-right text-text-faint tabular-nums">
                  {formatCurrency(row.averagePerAppointment)}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
};

// ── Service Revenue Table ─────────────────────────────────────────────────────────────────────────────

interface ServiceTableProps {
  data: ServiceRevenue[];
}

const ServiceRevenueTable: React.FC<ServiceTableProps> = ({ data }) => {
  if (data.length === 0) {
    return (
      <EmptyState
        title="Nema podataka"
        description="Za izabrani period nema prihoda po usluzi."
      />
    );
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-divider">
            <th className="text-left py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide">
              Usluga
            </th>
            <th className="text-left py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide hidden sm:table-cell">
              Kategorija
            </th>
            <th className="text-right py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide">
              Prihod
            </th>
            <th className="text-right py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide">
              Rezervacija
            </th>
            <th className="text-right py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide hidden sm:table-cell">
              Prosek
            </th>
          </tr>
        </thead>
        <tbody>
          {data.map((row, idx) => (
            <tr
              key={idx}
              className="border-b border-divider last:border-0 hover:bg-surface-2 transition-colors duration-150"
            >
              <td className="py-3 px-3 font-medium text-text">
                {row.serviceName}
              </td>
              <td className="py-3 px-3 text-text-muted hidden sm:table-cell">
                {row.category}
              </td>
              <td className="py-3 px-3 text-right font-semibold text-text tabular-nums">
                {formatCurrency(row.totalRevenue)}
              </td>
              <td className="py-3 px-3 text-right text-text-muted tabular-nums">
                {row.bookingCount}
              </td>
              <td className="py-3 px-3 text-right text-text-faint tabular-nums hidden sm:table-cell">
                {formatCurrency(row.averagePrice)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

// ── Page ────────────────────────────────────────────────────────────────────────────────────

export const ReportsPage: React.FC = () => {
  const [dateFrom, setDateFrom] = useState(daysAgo(30));
  const [dateTo, setDateTo] = useState(today());

  const summaryQuery = useQuery({
    queryKey: queryKeys.reports.summary(dateFrom, dateTo),
    queryFn: () => getReportSummary(dateFrom, dateTo),
    enabled: !!dateFrom && !!dateTo,
  });

  const staffQuery = useQuery({
    queryKey: queryKeys.reports.byStaff(dateFrom, dateTo),
    queryFn: () => getRevenueByStaff(dateFrom, dateTo),
    enabled: !!dateFrom && !!dateTo,
  });

  const serviceQuery = useQuery({
    queryKey: queryKeys.reports.byService(dateFrom, dateTo),
    queryFn: () => getRevenueByService(dateFrom, dateTo),
    enabled: !!dateFrom && !!dateTo,
  });

  const isLoading = summaryQuery.isLoading || staffQuery.isLoading || serviceQuery.isLoading;
  const summary = summaryQuery.data;
  const staffData: StaffRevenue[] = staffQuery.data ?? [];
  const serviceData: ServiceRevenue[] = serviceQuery.data ?? [];

  const handleRefresh = () => {
    summaryQuery.refetch();
    staffQuery.refetch();
    serviceQuery.refetch();
  };

  return (
    <div className="container-main py-6 space-y-6">

      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-end gap-4 justify-between">
        <div>
          <h1 className="text-xl font-semibold text-display text-text">Izveštaji</h1>
          <p className="text-xs text-text-faint mt-0.5">
            Analiza prihoda i performansi salona
          </p>
        </div>

        {/* Date range + refresh */}
        <div className="flex flex-wrap items-center gap-2">
          <div className="flex items-center gap-2">
            <label className="text-xs text-text-muted shrink-0">Od</label>
            <input
              type="date"
              value={dateFrom}
              max={dateTo}
              onChange={e => setDateFrom(e.target.value)}
              className="h-11 md:h-9 bg-surface border border-border rounded-lg md:rounded-md px-3
                text-base md:text-sm text-text
                focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary transition-interactive"
            />
          </div>
          <div className="flex items-center gap-2">
            <label className="text-xs text-text-muted shrink-0">Do</label>
            <input
              type="date"
              value={dateTo}
              min={dateFrom}
              onChange={e => setDateTo(e.target.value)}
              className="h-11 md:h-9 bg-surface border border-border rounded-lg md:rounded-md px-3
                text-base md:text-sm text-text
                focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary transition-interactive"
            />
          </div>
          <Button
            variant="secondary"
            size="sm"
            icon={<RefreshCw size={13} />}
            onClick={handleRefresh}
            loading={isLoading}
          >
            Osveži
          </Button>
        </div>
      </div>

      {/* Loading overlay for KPIs */}
      {summaryQuery.isLoading ? (
        <div className="flex justify-center py-8">
          <LoadingSpinner />
        </div>
      ) : summaryQuery.isError ? (
        <div className="p-3 bg-error-bg border border-error/20 rounded-lg text-sm text-error">
          Nije moguće učitati izveštaj. Pokušajte ponovo.
        </div>
      ) : summary ? (
        /* KPI Cards */
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <KpiCard
            label="Ukupan prihod"
            value={formatCurrency(summary.totalRevenue)}
            icon={<TrendingUp size={16} />}
            subtext="Za izabrani period"
          />
          <KpiCard
            label="Ukupno termina"
            value={summary.totalAppointments}
            icon={<Calendar size={16} />}
            subtext={`${summary.completedCount} završenih`}
          />
          <KpiCard
            label="Stopa otkazivanja"
            value={`${summary.cancellationRate.toFixed(1)}%`}
            icon={<XCircle size={16} />}
            subtext={`${summary.cancelledCount} otkazanih`}
          />
          <KpiCard
            label="Jedinstveni klijenti"
            value={summary.uniqueClients}
            icon={<Users size={16} />}
            subtext="U izabranom periodu"
          />
        </div>
      ) : null}

      {/* Revenue tables */}
      <div className="grid lg:grid-cols-2 gap-6">

        {/* Staff revenue */}
        <div className="card card-padded">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-sm font-semibold text-text">Prihod po zaposlenom</h2>
            {staffData.length > 0 && (
              <span className="text-xs text-text-faint">{staffData.length} zaposlenih</span>
            )}
          </div>
          {staffQuery.isLoading ? (
            <div className="flex justify-center py-8">
              <LoadingSpinner />
            </div>
          ) : (
            <StaffRevenueTable data={staffData} />
          )}
        </div>

        {/* Service revenue */}
        <div className="card card-padded">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-sm font-semibold text-text">Prihod po usluzi</h2>
            {serviceData.length > 0 && (
              <span className="text-xs text-text-faint">{serviceData.length} usluga</span>
            )}
          </div>
          {serviceQuery.isLoading ? (
            <div className="flex justify-center py-8">
              <LoadingSpinner />
            </div>
          ) : (
            <ServiceRevenueTable data={serviceData} />
          )}
        </div>

      </div>
    </div>
  );
};
