import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { format } from 'date-fns';
import {
  Building2, RefreshCw, CheckCircle2, Clock, AlertTriangle, XCircle,
  Users, UserCheck, Search
} from 'lucide-react';
import { getTenants } from '../api/tenants';
import type { TenantInfo } from '../types';
import { queryKeys } from '../lib/queryKeys';
import { KpiCard } from '../components/KpiCard';
import { Button } from '../components/Button';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { EmptyState } from '../components/EmptyState';

// ── Helpers ───────────────────────────────────────────────────────────────────

const formatDate = (iso?: string) => {
  if (!iso) return '—';
  try { return format(new Date(iso), 'dd.MM.yyyy HH:mm'); }
  catch { return iso; }
};

const formatDateShort = (iso?: string) => {
  if (!iso) return '—';
  try { return format(new Date(iso), 'dd.MM.yyyy'); }
  catch { return iso; }
};

const subStatusConfig: Record<string, { label: string; color: string; bg: string; icon: React.ReactNode }> = {
  'Aktivan':           { label: 'Aktivan',           color: 'text-emerald-600',  bg: 'bg-emerald-50 border-emerald-200', icon: <CheckCircle2 size={13} /> },
  'Probni':            { label: 'Probni',            color: 'text-blue-600',     bg: 'bg-blue-50 border-blue-200',       icon: <Clock size={13} /> },
  'Istekao':           { label: 'Istekao',           color: 'text-red-600',      bg: 'bg-red-50 border-red-200',         icon: <AlertTriangle size={13} /> },
  'ČekaVerifikaciju':  { label: 'Čeka verifikaciju', color: 'text-amber-600',    bg: 'bg-amber-50 border-amber-200',     icon: <XCircle size={13} /> },
};

const SubStatusBadge: React.FC<{ status: string }> = ({ status }) => {
  const cfg = subStatusConfig[status] ?? subStatusConfig['Istekao'];
  return (
    <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full border text-xs font-medium ${cfg.bg} ${cfg.color}`}>
      {cfg.icon}{cfg.label}
    </span>
  );
};

// ── Page ──────────────────────────────────────────────────────────────────────

export const TenantsPage: React.FC = () => {
  const [search, setSearch] = useState('');

  const { data, isLoading, isError, refetch, isFetching } = useQuery({
    queryKey: queryKeys.tenants.list(),
    queryFn: getTenants,
  });

  const tenants: TenantInfo[] = data ?? [];

  const filtered = search.trim()
    ? tenants.filter(t =>
        t.name.toLowerCase().includes(search.toLowerCase()) ||
        t.slug.toLowerCase().includes(search.toLowerCase()) ||
        (t.email && t.email.toLowerCase().includes(search.toLowerCase())) ||
        (t.city && t.city.toLowerCase().includes(search.toLowerCase()))
      )
    : tenants;

  const activeCount = tenants.filter(t => t.subscriptionStatus === 'Aktivan').length;
  const trialCount = tenants.filter(t => t.subscriptionStatus === 'Probni').length;
  const expiredCount = tenants.filter(t => t.subscriptionStatus === 'Istekao').length;
  const totalClients = tenants.reduce((sum, t) => sum + t.clientCount, 0);

  return (
    <div className="container-main py-6 space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-end gap-4 justify-between">
        <div>
          <h1 className="text-xl font-semibold text-display text-text">Saloni</h1>
          <p className="text-xs text-text-faint mt-0.5">Pregled svih registrovanih salona i pretplata</p>
        </div>
        <Button variant="secondary" size="sm" icon={<RefreshCw size={13} />} onClick={() => refetch()} loading={isFetching}>
          Osveži
        </Button>
      </div>

      {/* KPI */}
      {!isLoading && (
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <KpiCard label="Ukupno salona" value={tenants.length} icon={<Building2 size={16} />} subtext="Registrovani" />
          <KpiCard label="Aktivne pretplate" value={activeCount} icon={<CheckCircle2 size={16} />} subtext={trialCount > 0 ? `+ ${trialCount} probnih` : 'Plaćeni korisnici'} />
          <KpiCard label="Istekle pretplate" value={expiredCount} icon={<AlertTriangle size={16} />} subtext="Potrebno produženje" />
          <KpiCard label="Ukupno klijenata" value={totalClients} icon={<Users size={16} />} subtext="Svi saloni zajedno" />
        </div>
      )}

      {/* Search */}
      <div className="card card-padded">
        <div className="relative">
          <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-text-faint" />
          <input
            type="text"
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder="Pretraži po imenu, slug-u, emailu ili gradu..."
            className="w-full h-11 md:h-9 pl-9 pr-3 bg-surface border border-border rounded-lg md:rounded-md
              text-base md:text-sm text-text placeholder:text-text-faint
              focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary transition-interactive"
          />
        </div>
      </div>

      {/* Table */}
      <div className="card card-padded">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-sm font-semibold text-text">Pregled salona</h2>
          {filtered.length > 0 && (
            <span className="text-xs text-text-faint">{filtered.length} od {tenants.length}</span>
          )}
        </div>

        {isLoading ? (
          <div className="flex justify-center py-8"><LoadingSpinner /></div>
        ) : isError ? (
          <div className="p-3 bg-error-bg border border-error/20 rounded-lg text-sm text-error">
            Nije moguće učitati saloni. Pokušajte ponovo.
          </div>
        ) : filtered.length === 0 ? (
          <EmptyState title="Nema salona" description={search ? 'Nijedan salon ne odgovara pretrazi.' : 'Još uvek nema registrovanih salona.'} />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-divider">
                  <th className="text-left py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide">Salon</th>
                  <th className="text-center py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide">Status</th>
                  <th className="text-right py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide hidden sm:table-cell">Ističe</th>
                  <th className="text-right py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide hidden md:table-cell">Korisnici</th>
                  <th className="text-right py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide hidden md:table-cell">Klijenti</th>
                  <th className="text-right py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide">Poslednji login</th>
                  <th className="text-right py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide hidden lg:table-cell">Registrovan</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map(tenant => (
                  <tr
                    key={tenant.id}
                    className="border-b border-divider last:border-0 hover:bg-surface-2 transition-colors duration-150"
                  >
                    <td className="py-3 px-3">
                      <div>
                        <p className="font-medium text-text">{tenant.name}</p>
                        <p className="text-xs text-text-faint">{tenant.slug}{tenant.city ? ` · ${tenant.city}` : ''}</p>
                      </div>
                    </td>
                    <td className="py-3 px-3 text-center">
                      <SubStatusBadge status={tenant.subscriptionStatus} />
                    </td>
                    <td className="py-3 px-3 text-right text-text-muted tabular-nums hidden sm:table-cell whitespace-nowrap">
                      {tenant.subscriptionEndDate ? (
                        <span>
                          {formatDateShort(tenant.subscriptionEndDate)}
                          {tenant.daysRemaining !== undefined && tenant.daysRemaining !== null && (
                            <span className={`block text-[11px] ${tenant.daysRemaining <= 7 ? 'text-red-500' : 'text-text-faint'}`}>
                              {tenant.daysRemaining > 0 ? `još ${tenant.daysRemaining} dana` : 'isteklo'}
                            </span>
                          )}
                        </span>
                      ) : '—'}
                    </td>
                    <td className="py-3 px-3 text-right tabular-nums hidden md:table-cell">
                      <span className="inline-flex items-center gap-1 text-text-muted">
                        <UserCheck size={12} /> {tenant.userCount}
                      </span>
                    </td>
                    <td className="py-3 px-3 text-right tabular-nums hidden md:table-cell">
                      <span className="inline-flex items-center gap-1 text-text-muted">
                        <Users size={12} /> {tenant.clientCount}
                      </span>
                    </td>
                    <td className="py-3 px-3 text-right text-text-muted tabular-nums whitespace-nowrap">
                      {formatDate(tenant.lastLoginAt)}
                    </td>
                    <td className="py-3 px-3 text-right text-text-faint tabular-nums hidden lg:table-cell whitespace-nowrap">
                      {formatDateShort(tenant.createdAt)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};
