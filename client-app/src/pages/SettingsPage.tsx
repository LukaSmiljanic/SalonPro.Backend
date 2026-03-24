import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Save, Plus, Trash2, Award } from 'lucide-react';
import {
  getLoyaltyConfig,
  updateLoyaltyConfig,
} from '../api/settings';
import type { LoyaltyTierConfig } from '../types';
import { queryKeys } from '../lib/queryKeys';
import { Button } from '../components/Button';
import { LoadingSpinner } from '../components/LoadingSpinner';

// ── Helpers ────────────────────────────────────────────────────────────────────────────

const DEFAULT_TIERS: LoyaltyTierConfig[] = [
  { tierName: 'Bronze',   minVisits: 10,  benefit: '20% popusta' },
  { tierName: 'Silver',   minVisits: 25,  benefit: '30% popusta' },
  { tierName: 'Gold',     minVisits: 50,  benefit: 'Besplatna usluga' },
  { tierName: 'Platinum', minVisits: 100, benefit: 'Besplatna usluga + poklon' },
];

// ── Loyalty Section ────────────────────────────────────────────────────────────────────────────────

const LoyaltySection: React.FC = () => {
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
      // Also invalidate clients so loyalty badges refresh
      queryClient.invalidateQueries({ queryKey: queryKeys.clients.all });
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
        <p className="text-sm text-text-muted mb-4">
          Definišite nivoe lojalnosti na osnovu broja poseta. Broj poseta i pogodnosti se prikazuju na profilu klijenta.
        </p>

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

export const SettingsPage: React.FC = () => {
  return (
    <div className="container-main py-6 space-y-6">
      {/* Header */}
      <div className="flex items-center gap-3">
        <Award size={20} className="text-primary" />
        <div>
          <h1 className="text-xl font-semibold text-display text-text">Loyalty program</h1>
          <p className="text-xs text-text-faint mt-0.5">
            Konfigurišite nivoe lojalnosti i pogodnosti za klijente
          </p>
        </div>
      </div>

      <LoyaltySection />
    </div>
  );
};
