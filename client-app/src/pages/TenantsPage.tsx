import React, { useState, useRef, useEffect } from 'react';
import { createPortal } from 'react-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import {
  Building2, RefreshCw, CheckCircle2, Clock, AlertTriangle, XCircle,
  Users, UserCheck, Search, CalendarPlus, CreditCard, MoreVertical, Layers
} from 'lucide-react';
import { getTenants, extendSubscription, updateTenantPlan } from '../api/tenants';
import type { TenantInfo, TenantPlan } from '../types';
import { queryKeys } from '../lib/queryKeys';
import { KpiCard } from '../components/KpiCard';
import { Button } from '../components/Button';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { EmptyState } from '../components/EmptyState';
import { Modal } from '../components/Modal';
import { Input } from '../components/Input';

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

// ── Quick extend periods ──────────────────────────────────────────────────────

const extendOptions = [
  { label: '7 dana', days: 7 },
  { label: '30 dana', days: 30 },
  { label: '90 dana', days: 90 },
  { label: '365 dana', days: 365 },
];

const tenantPlans: TenantPlan[] = ['Basic', 'Standard', 'Pro'];

// ── Extend Subscription Modal ────────────────────────────────────────────────

interface ExtendModalProps {
  open: boolean;
  tenant: TenantInfo | null;
  onClose: () => void;
}

const ExtendSubscriptionModal: React.FC<ExtendModalProps> = ({ open, tenant, onClose }) => {
  const queryClient = useQueryClient();
  const [customDays, setCustomDays] = useState('');
  const [result, setResult] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: extendSubscription,
    onSuccess: (data) => {
      setResult(data.message);
      queryClient.invalidateQueries({ queryKey: queryKeys.tenants.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.payments.all });
    },
    onError: (err: unknown) => {
      const msg = (err as any)?.response?.data?.message ?? 'Greška pri produženju pretplate.';
      setError(typeof msg === 'string' ? msg : 'Greška pri produženju pretplate.');
    },
  });

  const handleExtend = (days: number) => {
    if (!tenant) return;
    setError(null);
    setResult(null);
    mutation.mutate({ tenantId: tenant.id, days });
  };

  const handleClose = () => {
    setCustomDays('');
    setResult(null);
    setError(null);
    onClose();
  };

  if (!tenant) return null;

  return (
    <Modal isOpen={open} title="Produži pretplatu" onClose={handleClose}>
      <div className="space-y-4">
        {/* Tenant info */}
        <div className="bg-surface-2 rounded-lg p-3">
          <p className="font-medium text-text text-sm">{tenant.name}</p>
          <p className="text-xs text-text-faint mt-0.5">{tenant.slug} {tenant.email ? `· ${tenant.email}` : ''}</p>
          <div className="flex items-center gap-3 mt-2">
            <SubStatusBadge status={tenant.subscriptionStatus} />
            {tenant.subscriptionEndDate && (
              <span className="text-xs text-text-muted">
                Ističe: {formatDateShort(tenant.subscriptionEndDate)}
                {tenant.daysRemaining != null && tenant.daysRemaining > 0 && (
                  <span className="text-text-faint"> (još {tenant.daysRemaining} dana)</span>
                )}
              </span>
            )}
          </div>
        </div>

        {error && (
          <div className="p-3 bg-error-bg border border-error/20 rounded-lg text-sm text-error">{error}</div>
        )}

        {result && (
          <div className="p-3 bg-emerald-50 border border-emerald-200 rounded-lg text-sm text-emerald-700">{result}</div>
        )}

        {/* Quick extend buttons */}
        <div>
          <label className="block text-xs font-medium text-text-muted mb-2">Brzo produženje</label>
          <div className="grid grid-cols-2 gap-2">
            {extendOptions.map(opt => (
              <button
                key={opt.days}
                onClick={() => handleExtend(opt.days)}
                disabled={mutation.isPending}
                className="flex items-center justify-center gap-2 px-3 py-2.5 bg-surface border border-border rounded-lg
                  text-sm font-medium text-text hover:border-primary hover:bg-primary/5
                  transition-colors disabled:opacity-50"
              >
                <CalendarPlus size={14} className="text-primary" />
                + {opt.label}
              </button>
            ))}
          </div>
        </div>

        {/* Custom days */}
        <div>
          <label className="block text-xs font-medium text-text-muted mb-1">Ili unesite broj dana</label>
          <div className="flex gap-2">
            <Input
              type="number"
              value={customDays}
              onChange={e => setCustomDays(e.target.value)}
              placeholder="npr. 45"
            />
            <Button
              onClick={() => {
                const days = parseInt(customDays);
                if (days > 0) handleExtend(days);
              }}
              disabled={!customDays || parseInt(customDays) <= 0}
              loading={mutation.isPending}
            >
              Produži
            </Button>
          </div>
        </div>

        <div className="flex justify-end pt-2">
          <Button variant="secondary" onClick={handleClose}>Zatvori</Button>
        </div>
      </div>
    </Modal>
  );
};

// ── Action Menu ──────────────────────────────────────────────────────────────

const TenantActions: React.FC<{
  tenant: TenantInfo;
  onExtend: (tenant: TenantInfo) => void;
  onChangePlan: (tenant: TenantInfo, plan: TenantPlan) => void;
}> = ({ tenant, onExtend, onChangePlan }) => {
  const [open, setOpen] = useState(false);
  const btnRef = useRef<HTMLButtonElement>(null);
  const [pos, setPos] = useState({ top: 0, left: 0 });

  useEffect(() => {
    if (open && btnRef.current) {
      const r = btnRef.current.getBoundingClientRect();
      setPos({ top: r.bottom + 4, left: r.right - 180 });
    }
  }, [open]);

  return (
    <>
      <button
        ref={btnRef}
        onClick={() => setOpen(v => !v)}
        className="p-1.5 rounded-md hover:bg-surface-2 text-text-faint hover:text-text transition-colors"
      >
        <MoreVertical size={15} />
      </button>
      {open && createPortal(
        <>
          <div className="fixed inset-0 z-[9998]" onClick={() => setOpen(false)} />
          <div
            className="fixed z-[9999] bg-surface border border-border rounded-lg shadow-xl py-1 min-w-[180px]"
            style={{ top: pos.top, left: Math.max(8, pos.left) }}
          >
            <button
              onClick={() => { onExtend(tenant); setOpen(false); }}
              className="w-full text-left px-3 py-2 text-xs flex items-center gap-2 hover:bg-surface-2 text-text transition-colors"
            >
              <CalendarPlus size={13} className="text-primary" />
              Produži pretplatu
            </button>
            <div className="border-t border-divider my-1" />
            <div className="px-3 pt-1 pb-1 text-[10px] uppercase tracking-wide text-text-faint">
              Paket
            </div>
            {tenantPlans.map(plan => (
              <button
                key={plan}
                onClick={() => { onChangePlan(tenant, plan); setOpen(false); }}
                className="w-full text-left px-3 py-2 text-xs flex items-center justify-between hover:bg-surface-2 text-text transition-colors"
              >
                <span className="inline-flex items-center gap-2">
                  <Layers size={12} className="text-primary" />
                  {plan}
                </span>
                {tenant.plan === plan && <span className="text-[10px] text-primary font-semibold">Aktivan</span>}
              </button>
            ))}
          </div>
        </>,
        document.body
      )}
    </>
  );
};

// ── Page ──────────────────────────────────────────────────────────────────────

export const TenantsPage: React.FC = () => {
  const [search, setSearch] = useState('');
  const [extendTenant, setExtendTenant] = useState<TenantInfo | null>(null);
  const [planResult, setPlanResult] = useState<string | null>(null);

  const queryClient = useQueryClient();

  const { data, isLoading, isError, refetch, isFetching } = useQuery({
    queryKey: queryKeys.tenants.list(),
    queryFn: getTenants,
  });

  const planMutation = useMutation({
    mutationFn: ({ tenantId, plan }: { tenantId: string; plan: TenantPlan }) => updateTenantPlan(tenantId, plan),
    onSuccess: (_, vars) => {
      setPlanResult(`Paket je ažuriran na ${vars.plan}.`);
      queryClient.invalidateQueries({ queryKey: queryKeys.tenants.all });
    },
    onError: () => {
      setPlanResult('Nije moguće ažurirati paket salona.');
    },
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

      {planResult && (
        <div className="p-3 bg-blue-50 border border-blue-200 rounded-lg text-sm text-blue-700 flex items-center justify-between">
          <span>{planResult}</span>
          <button className="text-xs underline ml-4" onClick={() => setPlanResult(null)}>Zatvori</button>
        </div>
      )}

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
                  <th className="text-center py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide">Paket</th>
                  <th className="text-right py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide">Poslednji login</th>
                  <th className="text-right py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide hidden lg:table-cell">Registrovan</th>
                  <th className="w-10 py-2 px-3"></th>
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
                        <p className="text-xs text-text-faint">{tenant.slug}{tenant.city ? ` · ${tenant.city}` : ''}{tenant.email ? ` · ${tenant.email}` : ''}</p>
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
                            <span className={`block text-[11px] ${tenant.daysRemaining <= 7 ? 'text-red-500 font-medium' : 'text-text-faint'}`}>
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
                    <td className="py-3 px-3 text-center">
                      <span className="inline-flex items-center px-2 py-1 rounded-full border text-xs bg-surface-2 border-border text-text">
                        {tenant.plan}
                      </span>
                    </td>
                    <td className="py-3 px-3 text-right text-text-muted tabular-nums whitespace-nowrap">
                      {formatDate(tenant.lastLoginAt)}
                    </td>
                    <td className="py-3 px-3 text-right text-text-faint tabular-nums hidden lg:table-cell whitespace-nowrap">
                      {formatDateShort(tenant.createdAt)}
                    </td>
                    <td className="py-3 px-1">
                      <TenantActions
                        tenant={tenant}
                        onExtend={setExtendTenant}
                        onChangePlan={(t, plan) => planMutation.mutate({ tenantId: t.id, plan })}
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Extend Subscription Modal */}
      <ExtendSubscriptionModal
        open={!!extendTenant}
        tenant={extendTenant}
        onClose={() => setExtendTenant(null)}
      />
    </div>
  );
};
