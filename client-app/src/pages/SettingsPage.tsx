import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Save, Plus, Trash2, Clock, Award } from 'lucide-react';
import {
  getWorkingHours,
  updateWorkingHours,
  getLoyaltyConfig,
  updateLoyaltyConfig,
} from '../api/settings';
import type { WorkingHoursEntry, LoyaltyTierConfig } from '../types';
import { queryKeys } from '../lib/queryKeys';
import { Button } from '../components/Button';
import { Input } from '../components/Input';
import { LoadingSpinner } from '../components/LoadingSpinner';

// ── Helpers ────────────────────────────────────────────────────────────────────────────

const DAY_NAMES = [
  'Nedelja',
  'Ponedeljak',
  'Utorak',
  'Sreda',
  'Četvrtak',
  'Petak',
  'Subota',
];

/** Ordered Mon–Sun (1..6, 0) */
const ORDERED_DAYS = [1, 2, 3, 4, 5, 6, 0];

/** Generate time options from startHour to endHour in 30-min steps */
function buildTimeOptions(startHour: number, endHour: number): string[] {
  const opts: string[] = [];
  for (let h = startHour; h <= endHour; h++) {
    opts.push(`${String(h).padStart(2, '0')}:00`);
    if (h < endHour) opts.push(`${String(h).padStart(2, '0')}:30`);
  }
  return opts;
}

const START_OPTIONS = buildTimeOptions(6, 22); // 06:00 – 22:00
const END_OPTIONS   = buildTimeOptions(6, 23); // 06:00 – 23:00

/** Normalise "HH:mm:ss" → "HH:mm" for <select> matching */
function toHHMM(t: string): string {
  return t?.slice(0, 5) ?? '09:00';
}

const DEFAULT_HOURS: WorkingHoursEntry[] = ORDERED_DAYS.map(dow => ({
  dayOfWeek: dow,
  startTime: '09:00:00',
  endTime: '18:00:00',
  isWorkingDay: dow >= 1 && dow <= 5,
}));

const DEFAULT_TIERS: LoyaltyTierConfig[] = [
  { tierName: 'Bronze',   minVisits: 10,  benefit: '20% popusta' },
  { tierName: 'Silver',   minVisits: 25,  benefit: '30% popusta' },
  { tierName: 'Gold',     minVisits: 50,  benefit: 'Besplatna usluga' },
  { tierName: 'Platinum', minVisits: 100, benefit: 'Besplatna usluga + poklon' },
];

// ── Tab button ───────────────────────────────────────────────────────────────────────────────

interface TabButtonProps {
  active: boolean;
  onClick: () => void;
  icon: React.ReactNode;
  label: string;
}

const TabButton: React.FC<TabButtonProps> = ({ active, onClick, icon, label }) => (
  <button
    onClick={onClick}
    className={`flex items-center gap-2 px-4 py-2.5 rounded-lg text-sm font-medium transition-all duration-200 min-h-[44px]
      ${active
        ? 'bg-primary text-white shadow-sm'
        : 'text-text-muted hover:bg-surface-2 hover:text-text active:bg-surface-offset'
      }`}
  >
    {icon}
    {label}
  </button>
);

// ── Toggle switch ─────────────────────────────────────────────────────────────────────────────────

interface ToggleProps {
  checked: boolean;
  onChange: (v: boolean) => void;
}

const Toggle: React.FC<ToggleProps> = ({ checked, onChange }) => (
  <button
    type="button"
    onClick={() => onChange(!checked)}
    className={`relative w-11 h-6 rounded-full transition-colors duration-200 shrink-0 ${
      checked ? 'bg-primary' : 'bg-border'
    }`}
    aria-checked={checked}
    role="switch"
  >
    <span
      className={`absolute top-1 left-1 w-4 h-4 rounded-full bg-white shadow-sm transition-transform duration-200 ${
        checked ? 'translate-x-5' : 'translate-x-0'
      }`}
    />
  </button>
);

// ── Working Hours Tab ────────────────────────────────────────────────────────────────────────────

const WorkingHoursTab: React.FC = () => {
  const queryClient = useQueryClient();
  const [hours, setHours] = useState<WorkingHoursEntry[] | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [initialised, setInitialised] = useState(false);

  const { data: fetchedHours, isLoading } = useQuery({
    queryKey: queryKeys.settings.workingHours(),
    queryFn: getWorkingHours,
  });

  // Initialise local state from fetched data (only once)
  React.useEffect(() => {
    if (initialised) return;
    if (isLoading) return;
    if (fetchedHours && fetchedHours.length > 0) {
      const byDow: Record<number, WorkingHoursEntry> = {};
      fetchedHours.forEach(e => { byDow[e.dayOfWeek] = e; });
      setHours(ORDERED_DAYS.map(dow => byDow[dow] ?? DEFAULT_HOURS.find(d => d.dayOfWeek === dow)!));
    } else {
      setHours([...DEFAULT_HOURS]);
    }
    setInitialised(true);
  }, [fetchedHours, isLoading, initialised]);

  const saveMutation = useMutation({
    mutationFn: updateWorkingHours,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.settings.workingHours() });
      setSuccessMsg('Radno vreme je sačuvano.');
      setErrorMsg(null);
      setTimeout(() => setSuccessMsg(null), 3000);
    },
    onError: () => {
      setErrorMsg('Nije moguće sačuvati radno vreme. Pokušajte ponovo.');
      setSuccessMsg(null);
    },
  });

  const updateDay = (idx: number, patch: Partial<WorkingHoursEntry>) => {
    setHours(prev => {
      if (!prev) return prev;
      const updated = [...prev];
      updated[idx] = { ...updated[idx], ...patch };
      return updated;
    });
  };

  const handleSave = () => {
    if (!hours) return;
    // Normalise HH:mm → HH:mm:00
    const payload = hours.map(h => ({
      ...h,
      startTime: h.startTime.length === 5 ? `${h.startTime}:00` : h.startTime,
      endTime:   h.endTime.length   === 5 ? `${h.endTime}:00`   : h.endTime,
    }));
    saveMutation.mutate(payload);
  };

  if (isLoading || !hours) {
    return (
      <div className="flex justify-center py-16">
        <LoadingSpinner />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {successMsg && (
        <div className="p-3 bg-[#edf7f2] border border-[#2d7a4f]/20 rounded-lg text-sm text-[#2d7a4f]">
          {successMsg}
        </div>
      )}
      {errorMsg && (
        <div className="p-3 bg-error-bg border border-error/20 rounded-lg text-sm text-error">
          {errorMsg}
        </div>
      )}

      <div className="card card-padded">
        <ul className="space-y-2">
          {hours.map((entry, idx) => (
            <li
              key={entry.dayOfWeek}
              className="flex flex-col sm:flex-row sm:items-center gap-3 py-3 px-4 rounded-lg bg-surface-2"
            >
              {/* Day name */}
              <span className="w-28 text-sm font-medium text-text shrink-0">
                {DAY_NAMES[entry.dayOfWeek]}
              </span>

              {/* Toggle */}
              <div className="flex items-center gap-2 shrink-0">
                <Toggle
                  checked={entry.isWorkingDay}
                  onChange={v => updateDay(idx, { isWorkingDay: v })}
                />
                <span className={`text-xs font-medium ${entry.isWorkingDay ? 'text-primary' : 'text-text-faint'}`}>
                  {entry.isWorkingDay ? 'Radni dan' : 'Neradni dan'}
                </span>
              </div>

              {/* Time selects */}
              <div className="flex items-center gap-2 flex-1 sm:justify-end">
                <select
                  value={toHHMM(entry.startTime)}
                  onChange={e => updateDay(idx, { startTime: e.target.value })}
                  disabled={!entry.isWorkingDay}
                  className={`h-11 md:h-9 bg-surface border rounded-lg md:rounded-md px-2 text-base md:text-sm text-text
                    focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary transition-interactive
                    ${entry.isWorkingDay ? 'border-border' : 'border-border opacity-40 cursor-not-allowed'}
                  `}
                >
                  {START_OPTIONS.map(t => (
                    <option key={t} value={t}>{t}</option>
                  ))}
                </select>

                <span className="text-text-faint text-sm shrink-0">—</span>

                <select
                  value={toHHMM(entry.endTime)}
                  onChange={e => updateDay(idx, { endTime: e.target.value })}
                  disabled={!entry.isWorkingDay}
                  className={`h-11 md:h-9 bg-surface border rounded-lg md:rounded-md px-2 text-base md:text-sm text-text
                    focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary transition-interactive
                    ${entry.isWorkingDay ? 'border-border' : 'border-border opacity-40 cursor-not-allowed'}
                  `}
                >
                  {END_OPTIONS.map(t => (
                    <option key={t} value={t}>{t}</option>
                  ))}
                </select>
              </div>
            </li>
          ))}
        </ul>
      </div>

      <div className="flex justify-end">
        <Button
          icon={<Save size={14} />}
          onClick={handleSave}
          loading={saveMutation.isPending}
        >
          Sačuvaj
        </Button>
      </div>
    </div>
  );
};

// ── Loyalty Tab ────────────────────────────────────────────────────────────────────────────────

const LoyaltyTab: React.FC = () => {
  const queryClient = useQueryClient();
  const [tiers, setTiers] = useState<LoyaltyTierConfig[] | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [initialised, setInitialised] = useState(false);

  const { data: fetchedTiers, isLoading } = useQuery({
    queryKey: queryKeys.settings.loyalty(),
    queryFn: getLoyaltyConfig,
  });

  React.useEffect(() => {
    if (initialised) return;
    if (isLoading) return;
    setTiers(fetchedTiers && fetchedTiers.length > 0 ? fetchedTiers : [...DEFAULT_TIERS]);
    setInitialised(true);
  }, [fetchedTiers, isLoading, initialised]);

  const saveMutation = useMutation({
    mutationFn: updateLoyaltyConfig,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.settings.loyalty() });
      setSuccessMsg('Loyalty program je sačuvan.');
      setErrorMsg(null);
      setTimeout(() => setSuccessMsg(null), 3000);
    },
    onError: () => {
      setErrorMsg('Nije moguće sačuvati loyalty program. Pokušajte ponovo.');
      setSuccessMsg(null);
    },
  });

  const updateTier = (idx: number, patch: Partial<LoyaltyTierConfig>) => {
    setTiers(prev => {
      if (!prev) return prev;
      const updated = [...prev];
      updated[idx] = { ...updated[idx], ...patch };
      return updated;
    });
  };

  const addTier = () => {
    setTiers(prev => [
      ...(prev ?? []),
      { tierName: '', minVisits: 0, benefit: '' },
    ]);
  };

  const removeTier = (idx: number) => {
    setTiers(prev => prev ? prev.filter((_, i) => i !== idx) : prev);
  };

  const handleSave = () => {
    if (!tiers) return;
    saveMutation.mutate(tiers);
  };

  if (isLoading || !tiers) {
    return (
      <div className="flex justify-center py-16">
        <LoadingSpinner />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {successMsg && (
        <div className="p-3 bg-[#edf7f2] border border-[#2d7a4f]/20 rounded-lg text-sm text-[#2d7a4f]">
          {successMsg}
        </div>
      )}
      {errorMsg && (
        <div className="p-3 bg-error-bg border border-error/20 rounded-lg text-sm text-error">
          {errorMsg}
        </div>
      )}

      <div className="card card-padded">
        {/* Table header — desktop only */}
        <div className="hidden sm:grid grid-cols-[1fr_120px_1fr_44px] gap-3 mb-2 px-4">
          <span className="text-xs font-medium text-text-muted uppercase tracking-wide">Nivo</span>
          <span className="text-xs font-medium text-text-muted uppercase tracking-wide">Min. poseta</span>
          <span className="text-xs font-medium text-text-muted uppercase tracking-wide">Pogodnost</span>
          <span />
        </div>

        <ul className="space-y-2">
          {tiers.map((tier, idx) => (
            <li
              key={idx}
              className="flex flex-col sm:grid sm:grid-cols-[1fr_120px_1fr_44px] gap-3 items-start sm:items-center
                py-3 px-4 rounded-lg bg-surface-2"
            >
              {/* Tier name */}
              <div>
                <span className="sm:hidden text-xs text-text-faint mb-1 block">Nivo</span>
                <input
                  value={tier.tierName}
                  onChange={e => updateTier(idx, { tierName: e.target.value })}
                  placeholder="npr. Bronze"
                  className="w-full h-11 md:h-9 bg-surface border border-border rounded-lg md:rounded-md px-3
                    text-base md:text-sm text-text placeholder:text-text-faint
                    focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary transition-interactive"
                />
              </div>

              {/* Min visits */}
              <div>
                <span className="sm:hidden text-xs text-text-faint mb-1 block">Min. poseta</span>
                <input
                  type="number"
                  min={0}
                  value={tier.minVisits}
                  onChange={e => updateTier(idx, { minVisits: Number(e.target.value) })}
                  placeholder="10"
                  className="w-full h-11 md:h-9 bg-surface border border-border rounded-lg md:rounded-md px-3
                    text-base md:text-sm text-text placeholder:text-text-faint
                    focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary transition-interactive"
                />
              </div>

              {/* Benefit */}
              <div>
                <span className="sm:hidden text-xs text-text-faint mb-1 block">Pogodnost</span>
                <input
                  value={tier.benefit}
                  onChange={e => updateTier(idx, { benefit: e.target.value })}
                  placeholder="npr. 20% popusta"
                  className="w-full h-11 md:h-9 bg-surface border border-border rounded-lg md:rounded-md px-3
                    text-base md:text-sm text-text placeholder:text-text-faint
                    focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary transition-interactive"
                />
              </div>

              {/* Remove */}
              <button
                onClick={() => removeTier(idx)}
                className="p-3 md:p-2.5 rounded-lg text-text-muted hover:text-error hover:bg-error-bg
                  active:bg-error-bg transition-all duration-200 min-h-[44px] min-w-[44px] flex items-center justify-center"
                title="Ukloni nivo"
              >
                <Trash2 size={16} className="md:w-3.5 md:h-3.5" />
              </button>
            </li>
          ))}
        </ul>

        <button
          onClick={addTier}
          className="mt-3 flex items-center gap-2 w-full py-3 px-4 rounded-lg border border-dashed border-border
            text-sm text-text-muted hover:text-primary hover:border-primary hover:bg-primary/5
            active:bg-primary/10 transition-all duration-200 min-h-[44px]"
        >
          <Plus size={15} />
          Dodaj nivo
        </button>
      </div>

      <div className="flex justify-end">
        <Button
          icon={<Save size={14} />}
          onClick={handleSave}
          loading={saveMutation.isPending}
        >
          Sačuvaj
        </Button>
      </div>
    </div>
  );
};

// ── Page ────────────────────────────────────────────────────────────────────────────────────

type ActiveTab = 'working-hours' | 'loyalty';

export const SettingsPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<ActiveTab>('working-hours');

  return (
    <div className="container-main py-6 space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-xl font-semibold text-display text-text">Podešavanja</h1>
        <p className="text-xs text-text-faint mt-0.5">
          Konfigurišite radno vreme i loyalty program salona
        </p>
      </div>

      {/* Tab bar */}
      <div className="flex items-center gap-2">
        <TabButton
          active={activeTab === 'working-hours'}
          onClick={() => setActiveTab('working-hours')}
          icon={<Clock size={15} />}
          label="Radno vreme"
        />
        <TabButton
          active={activeTab === 'loyalty'}
          onClick={() => setActiveTab('loyalty')}
          icon={<Award size={15} />}
          label="Loyalty program"
        />
      </div>

      {/* Tab content */}
      {activeTab === 'working-hours' ? <WorkingHoursTab /> : <LoyaltyTab />}
    </div>
  );
};
