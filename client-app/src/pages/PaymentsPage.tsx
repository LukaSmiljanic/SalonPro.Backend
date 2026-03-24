import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import {
  CreditCard, RefreshCw, CheckCircle2, Clock, AlertTriangle, XCircle,
  ChevronDown, Trash2, TrendingUp
} from 'lucide-react';
import {
  getPayments, getPaymentSummary, createPayment,
  updatePaymentStatus, deletePayment
} from '../api/payments';
import { getTenants } from '../api/tenants';
import type { Payment, PaymentStatus, PaymentSummary, TenantInfo } from '../types';
import { queryKeys } from '../lib/queryKeys';
import { KpiCard } from '../components/KpiCard';
import { Button } from '../components/Button';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { EmptyState } from '../components/EmptyState';
import { Modal } from '../components/Modal';
import { Input } from '../components/Input';

// ── Helpers ───────────────────────────────────────────────────────────────────

const formatCurrency = (value: number, currency = 'RSD') =>
  new Intl.NumberFormat('sr-Latn-RS', {
    style: 'currency',
    currency,
    minimumFractionDigits: 0,
  }).format(value);

const formatDate = (iso: string) => {
  try { return format(new Date(iso), 'dd.MM.yyyy'); }
  catch { return iso; }
};

const statusConfig: Record<PaymentStatus, { label: string; color: string; bg: string; icon: React.ReactNode }> = {
  Pending:   { label: 'Na čekanju',  color: 'text-amber-600',  bg: 'bg-amber-50 border-amber-200',  icon: <Clock size={13} /> },
  Paid:      { label: 'Plaćeno',     color: 'text-emerald-600',bg: 'bg-emerald-50 border-emerald-200',icon: <CheckCircle2 size={13} /> },
  Overdue:   { label: 'Zakasnelo',   color: 'text-red-600',    bg: 'bg-red-50 border-red-200',       icon: <AlertTriangle size={13} /> },
  Cancelled: { label: 'Otkazano',    color: 'text-text-faint', bg: 'bg-surface-2 border-border',     icon: <XCircle size={13} /> },
};

const statusOptions: PaymentStatus[] = ['Pending', 'Paid', 'Overdue', 'Cancelled'];

const currentYear = new Date().getFullYear();
const yearOptions = Array.from({ length: 5 }, (_, i) => currentYear - i);
const monthNames = [
  'Januar', 'Februar', 'Mart', 'April', 'Maj', 'Jun',
  'Jul', 'Avgust', 'Septembar', 'Oktobar', 'Novembar', 'Decembar',
];

// ── Status Badge ──────────────────────────────────────────────────────────────

const StatusBadge: React.FC<{ status: PaymentStatus }> = ({ status }) => {
  const cfg = statusConfig[status];
  return (
    <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full border text-xs font-medium ${cfg.bg} ${cfg.color}`}>
      {cfg.icon}{cfg.label}
    </span>
  );
};

// ── Status Dropdown ───────────────────────────────────────────────────────────

const StatusDropdown: React.FC<{
  current: PaymentStatus;
  onSelect: (status: PaymentStatus) => void;
  isLoading: boolean;
}> = ({ current, onSelect, isLoading }) => {
  const [open, setOpen] = useState(false);

  return (
    <div className="relative">
      <button
        onClick={() => setOpen(v => !v)}
        disabled={isLoading}
        className="inline-flex items-center gap-1.5 text-xs text-primary hover:text-primary/80 transition-colors disabled:opacity-50"
      >
        Promeni <ChevronDown size={12} className={open ? 'rotate-180 transition-transform' : 'transition-transform'} />
      </button>
      {open && (
        <>
          <div className="fixed inset-0 z-40" onClick={() => setOpen(false)} />
          <div className="absolute right-0 top-full mt-1 z-50 bg-surface border border-border rounded-lg shadow-xl py-1 min-w-[140px]">
            {statusOptions.map(s => (
              <button
                key={s}
                onClick={() => { onSelect(s); setOpen(false); }}
                disabled={s === current}
                className={`w-full text-left px-3 py-2 text-xs flex items-center gap-2 transition-colors
                  ${s === current ? 'bg-surface-2 text-text-faint cursor-default' : 'hover:bg-surface-2 text-text'}`}
              >
                {statusConfig[s].icon}
                {statusConfig[s].label}
              </button>
            ))}
          </div>
        </>
      )}
    </div>
  );
};

// ── Create Payment Modal ──────────────────────────────────────────────────────

interface CreatePaymentModalProps {
  open: boolean;
  onClose: () => void;
  tenants: TenantInfo[];
}

const CreatePaymentModal: React.FC<CreatePaymentModalProps> = ({ open, onClose, tenants }) => {
  const queryClient = useQueryClient();
  const [tenantId, setTenantId] = useState('');
  const [amount, setAmount] = useState('');
  const [currency, setCurrency] = useState('RSD');
  const [periodStart, setPeriodStart] = useState('');
  const [periodEnd, setPeriodEnd] = useState('');
  const [notes, setNotes] = useState('');
  const [error, setError] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: createPayment,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.payments.all });
      resetAndClose();
    },
    onError: (err: unknown) => {
      const message = (err as any)?.response?.data?.detail ?? (err as any)?.message;
      setError(typeof message === 'string' ? message : 'Nije moguće kreirati plaćanje.');
    },
  });

  const resetAndClose = () => {
    setTenantId(''); setAmount(''); setCurrency('RSD');
    setPeriodStart(''); setPeriodEnd(''); setNotes('');
    setError(null); onClose();
  };

  const handleSubmit = () => {
    if (!tenantId || !amount || !periodStart || !periodEnd) {
      setError('Popunite sva obavezna polja.');
      return;
    }
    setError(null);
    mutation.mutate({
      tenantId,
      amount: parseFloat(amount),
      currency,
      periodStart: new Date(periodStart).toISOString(),
      periodEnd: new Date(periodEnd).toISOString(),
      notes: notes || undefined,
    });
  };

  return (
    <Modal isOpen={open} title="Novo plaćanje" onClose={resetAndClose}>
      <div className="space-y-4">
        {error && (
          <div className="p-3 bg-error-bg border border-error/20 rounded-lg text-sm text-error">{error}</div>
        )}

        {/* Tenant select */}
        <div>
          <label className="block text-xs font-medium text-text-muted mb-1">Salon</label>
          <select
            value={tenantId}
            onChange={e => setTenantId(e.target.value)}
            className="w-full h-11 md:h-9 bg-surface border border-border rounded-lg md:rounded-md px-3
              text-base md:text-sm text-text
              focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary transition-interactive"
          >
            <option value="">Izaberite salon...</option>
            {tenants.map(t => (
              <option key={t.id} value={t.id}>{t.name}</option>
            ))}
          </select>
        </div>

        <div className="grid grid-cols-2 gap-3">
          <Input label="Iznos" type="number" value={amount} onChange={e => setAmount(e.target.value)} placeholder="0" />
          <Input label="Valuta" value={currency} onChange={e => setCurrency(e.target.value)} placeholder="RSD" />
        </div>

        <div className="grid grid-cols-2 gap-3">
          <Input label="Period od" type="date" value={periodStart} onChange={e => setPeriodStart(e.target.value)} />
          <Input label="Period do" type="date" value={periodEnd} onChange={e => setPeriodEnd(e.target.value)} />
        </div>

        <Input label="Napomene" value={notes} onChange={e => setNotes(e.target.value)} placeholder="Opcionalno..." />

        <div className="flex justify-end gap-2 pt-2">
          <Button variant="secondary" onClick={resetAndClose}>Otkaži</Button>
          <Button onClick={handleSubmit} loading={mutation.isPending}>Kreiraj</Button>
        </div>
      </div>
    </Modal>
  );
};

// ── Main Page ─────────────────────────────────────────────────────────────────

export const PaymentsPage: React.FC = () => {
  const queryClient = useQueryClient();
  const [filterYear, setFilterYear] = useState<number | undefined>(currentYear);
  const [filterMonth, setFilterMonth] = useState<number | undefined>(undefined);
  const [filterStatus, setFilterStatus] = useState<PaymentStatus | undefined>(undefined);
  const [showCreate, setShowCreate] = useState(false);

  const paymentsQuery = useQuery({
    queryKey: queryKeys.payments.list({ year: filterYear, month: filterMonth, status: filterStatus }),
    queryFn: () => getPayments({ year: filterYear, month: filterMonth, status: filterStatus }),
  });

  const summaryQuery = useQuery({
    queryKey: queryKeys.payments.summary(),
    queryFn: getPaymentSummary,
  });

  const tenantsQuery = useQuery({
    queryKey: queryKeys.tenants.list(),
    queryFn: getTenants,
  });

  const statusMutation = useMutation({
    mutationFn: updatePaymentStatus,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.payments.all });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: deletePayment,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.payments.all });
    },
  });

  const payments: Payment[] = paymentsQuery.data ?? [];
  const summaries: PaymentSummary[] = summaryQuery.data ?? [];
  const allTenants: TenantInfo[] = tenantsQuery.data ?? [];
  const isLoading = paymentsQuery.isLoading;

  // Aggregate KPI from summaries
  const totalPaid = summaries.reduce((acc, s) => acc + s.totalPaid, 0);
  const totalPending = summaries.reduce((acc, s) => acc + s.totalPending, 0);
  const totalTenants = summaries.length;

  const handleStatusChange = (paymentId: string, newStatus: PaymentStatus) => {
    statusMutation.mutate({ id: paymentId, status: newStatus });
  };

  const handleDelete = (payment: Payment) => {
    if (window.confirm(`Obrisati plaćanje za ${payment.tenantName}?`)) {
      deleteMutation.mutate(payment.id);
    }
  };

  return (
    <div className="container-main py-6 space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-end gap-4 justify-between">
        <div>
          <h1 className="text-xl font-semibold text-display text-text">Plaćanja</h1>
          <p className="text-xs text-text-faint mt-0.5">Upravljanje pretplatama i plaćanjima salona</p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="secondary" size="sm" icon={<RefreshCw size={13} />} onClick={() => {
            paymentsQuery.refetch();
            summaryQuery.refetch();
          }} loading={paymentsQuery.isFetching}>
            Osveži
          </Button>
          <Button size="sm" icon={<CreditCard size={13} />} onClick={() => setShowCreate(true)}>
            Novo plaćanje
          </Button>
        </div>
      </div>

      {/* KPI Cards */}
      {!summaryQuery.isLoading && (
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <KpiCard
            label="Ukupno plaćeno"
            value={formatCurrency(totalPaid)}
            icon={<CheckCircle2 size={16} />}
            subtext="Sva potvrđena plaćanja"
          />
          <KpiCard
            label="Na čekanju"
            value={formatCurrency(totalPending)}
            icon={<Clock size={16} />}
            subtext="Neplaćeno + zakasnelo"
          />
          <KpiCard
            label="Ukupno salona"
            value={totalTenants}
            icon={<TrendingUp size={16} />}
            subtext="Aktivni korisnici"
          />
          <KpiCard
            label="Ovaj mesec"
            value={payments.filter(p => p.status === 'Paid').length}
            icon={<CreditCard size={16} />}
            subtext={`od ${payments.length} plaćanja`}
          />
        </div>
      )}

      {/* Filters */}
      <div className="card card-padded">
        <div className="flex flex-wrap items-center gap-3">
          <div className="flex items-center gap-2">
            <label className="text-xs text-text-muted shrink-0">Godina</label>
            <select
              value={filterYear ?? ''}
              onChange={e => setFilterYear(e.target.value ? parseInt(e.target.value) : undefined)}
              className="h-11 md:h-9 bg-surface border border-border rounded-lg md:rounded-md px-3
                text-base md:text-sm text-text
                focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary transition-interactive"
            >
              <option value="">Sve</option>
              {yearOptions.map(y => <option key={y} value={y}>{y}</option>)}
            </select>
          </div>
          <div className="flex items-center gap-2">
            <label className="text-xs text-text-muted shrink-0">Mesec</label>
            <select
              value={filterMonth ?? ''}
              onChange={e => setFilterMonth(e.target.value ? parseInt(e.target.value) : undefined)}
              className="h-11 md:h-9 bg-surface border border-border rounded-lg md:rounded-md px-3
                text-base md:text-sm text-text
                focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary transition-interactive"
            >
              <option value="">Svi</option>
              {monthNames.map((name, i) => <option key={i} value={i + 1}>{name}</option>)}
            </select>
          </div>
          <div className="flex items-center gap-2">
            <label className="text-xs text-text-muted shrink-0">Status</label>
            <select
              value={filterStatus ?? ''}
              onChange={e => setFilterStatus(e.target.value ? e.target.value as PaymentStatus : undefined)}
              className="h-11 md:h-9 bg-surface border border-border rounded-lg md:rounded-md px-3
                text-base md:text-sm text-text
                focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary transition-interactive"
            >
              <option value="">Svi</option>
              {statusOptions.map(s => <option key={s} value={s}>{statusConfig[s].label}</option>)}
            </select>
          </div>
        </div>
      </div>

      {/* Payments Table */}
      <div className="card card-padded">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-sm font-semibold text-text">Pregled plaćanja</h2>
          {payments.length > 0 && (
            <span className="text-xs text-text-faint">{payments.length} stavki</span>
          )}
        </div>

        {isLoading ? (
          <div className="flex justify-center py-8">
            <LoadingSpinner />
          </div>
        ) : paymentsQuery.isError ? (
          <div className="p-3 bg-error-bg border border-error/20 rounded-lg text-sm text-error">
            Nije moguće učitati plaćanja. Pokušajte ponovo.
          </div>
        ) : payments.length === 0 ? (
          <EmptyState
            title="Nema plaćanja"
            description="Za izabrane filtere nema podataka o plaćanjima."
          />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-divider">
                  <th className="text-left py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide">Salon</th>
                  <th className="text-right py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide">Iznos</th>
                  <th className="text-left py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide hidden sm:table-cell">Period</th>
                  <th className="text-center py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide">Status</th>
                  <th className="text-left py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide hidden md:table-cell">Napomene</th>
                  <th className="text-right py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide">Akcije</th>
                </tr>
              </thead>
              <tbody>
                {payments.map(payment => (
                  <tr
                    key={payment.id}
                    className="border-b border-divider last:border-0 hover:bg-surface-2 transition-colors duration-150"
                  >
                    <td className="py-3 px-3 font-medium text-text">{payment.tenantName}</td>
                    <td className="py-3 px-3 text-right font-semibold text-text tabular-nums">
                      {formatCurrency(payment.amount, payment.currency)}
                    </td>
                    <td className="py-3 px-3 text-text-muted hidden sm:table-cell whitespace-nowrap">
                      {formatDate(payment.periodStart)} — {formatDate(payment.periodEnd)}
                    </td>
                    <td className="py-3 px-3 text-center">
                      <StatusBadge status={payment.status} />
                    </td>
                    <td className="py-3 px-3 text-text-faint text-xs hidden md:table-cell max-w-[200px] truncate">
                      {payment.notes || '—'}
                    </td>
                    <td className="py-3 px-3 text-right">
                      <div className="flex items-center justify-end gap-3">
                        <StatusDropdown
                          current={payment.status}
                          onSelect={(s) => handleStatusChange(payment.id, s)}
                          isLoading={statusMutation.isPending}
                        />
                        <button
                          onClick={() => handleDelete(payment)}
                          className="text-text-faint hover:text-error transition-colors"
                          title="Obriši"
                        >
                          <Trash2 size={14} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Tenant Summary Table */}
      {summaries.length > 0 && (
        <div className="card card-padded">
          <h2 className="text-sm font-semibold text-text mb-4">Pregled po salonu</h2>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-divider">
                  <th className="text-left py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide">Salon</th>
                  <th className="text-right py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide">Plaćeno</th>
                  <th className="text-right py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide">Neplaćeno</th>
                  <th className="text-right py-2 px-3 text-xs font-medium text-text-muted uppercase tracking-wide hidden sm:table-cell">Poslednje plaćanje</th>
                </tr>
              </thead>
              <tbody>
                {summaries.map(s => (
                  <tr key={s.tenantId} className="border-b border-divider last:border-0 hover:bg-surface-2 transition-colors duration-150">
                    <td className="py-3 px-3 font-medium text-text">{s.tenantName}</td>
                    <td className="py-3 px-3 text-right font-semibold text-emerald-600 tabular-nums">{formatCurrency(s.totalPaid)}</td>
                    <td className="py-3 px-3 text-right font-semibold text-amber-600 tabular-nums">{formatCurrency(s.totalPending)}</td>
                    <td className="py-3 px-3 text-right text-text-faint tabular-nums hidden sm:table-cell">
                      {s.lastPaymentDate ? formatDate(s.lastPaymentDate) : '—'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Create Payment Modal */}
      <CreatePaymentModal
        open={showCreate}
        onClose={() => setShowCreate(false)}
        tenants={allTenants}
      />
    </div>
  );
};
