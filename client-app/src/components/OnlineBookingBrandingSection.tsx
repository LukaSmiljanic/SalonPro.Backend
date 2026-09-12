import React, { useEffect, useRef, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Copy, Globe, ImageIcon, Save } from 'lucide-react';
import { toast } from 'sonner';
import { useAuth } from '../hooks/useAuth';
import {
  getTenantBranding,
  updateTenantBranding,
  uploadTenantLogo,
} from '../api/settings';
import { queryKeys } from '../lib/queryKeys';
import { resolveSocialImageUrl } from '../lib/mediaUrl';
import { Button } from './Button';
import { LoadingSpinner } from './LoadingSpinner';

export const OnlineBookingBrandingSection: React.FC = () => {
  const { features } = useAuth();
  const queryClient = useQueryClient();
  const logoInputRef = useRef<HTMLInputElement>(null);

  const { data: branding, isLoading } = useQuery({
    queryKey: queryKeys.settings.branding(),
    queryFn: getTenantBranding,
  });

  const [primaryColor, setPrimaryColor] = useState('#5b3a8c');
  const [accentColor, setAccentColor] = useState('#8b5cf6');
  const [onlineEnabled, setOnlineEnabled] = useState(true);
  const [initialized, setInitialized] = useState(false);

  useEffect(() => {
    if (!branding || initialized) return;
    setPrimaryColor(branding.primaryColor ?? '#5b3a8c');
    setAccentColor(branding.accentColor ?? '#8b5cf6');
    setOnlineEnabled(branding.onlineBookingEnabled);
    setInitialized(true);
  }, [branding, initialized]);

  const saveMutation = useMutation({
    mutationFn: () => updateTenantBranding({
      primaryColor,
      accentColor,
      onlineBookingEnabled: onlineEnabled,
    }),
    onSuccess: (data) => {
      queryClient.setQueryData(queryKeys.settings.branding(), data);
      toast.success('Brending je sačuvan.');
    },
    onError: () => toast.error('Čuvanje nije uspelo.'),
  });

  const logoMutation = useMutation({
    mutationFn: uploadTenantLogo,
    onSuccess: (data) => {
      queryClient.setQueryData(queryKeys.settings.branding(), data);
      if (logoInputRef.current) logoInputRef.current.value = '';
      toast.success('Logo je uploadovan.');
    },
    onError: () => toast.error('Upload logotipa nije uspeo.'),
  });

  const copyLink = async () => {
    if (!branding?.publicBookingUrl) return;
    try {
      await navigator.clipboard.writeText(branding.publicBookingUrl);
      toast.success('Link je kopiran.');
    } catch {
      toast.error('Kopiranje nije uspelo.');
    }
  };

  if (isLoading || !branding) {
    return (
      <div className="flex justify-center py-12">
        <LoadingSpinner />
      </div>
    );
  }

  const logoSrc = resolveSocialImageUrl(branding.logoUrl);
  const canBook = branding.canUseOnlineBooking && features.canUseOnlineBooking;

  return (
    <div className="card card-padded space-y-5">
      <div className="flex items-center gap-2">
        <Globe size={18} className="text-primary" />
        <div>
          <h2 className="text-base font-semibold text-text">Online rezervacije i brending</h2>
          <p className="text-xs text-text-faint mt-0.5">
            Javna stranica za zakazivanje — logo i boje salona
          </p>
        </div>
      </div>

      {!canBook && (
        <p className="text-sm text-warning bg-warning-bg border border-warning/20 rounded-lg p-3">
          Online rezervacije zahtevaju <strong>Standard</strong> ili <strong>Pro</strong> paket.
        </p>
      )}

      <div className="flex flex-col sm:flex-row gap-4 items-start">
        <div className="w-24 h-24 rounded-xl border border-border bg-surface-2 flex items-center justify-center overflow-hidden shrink-0">
          {logoSrc ? (
            <img src={logoSrc} alt="Logo" className="w-full h-full object-contain p-2" />
          ) : (
            <ImageIcon size={28} className="text-text-faint" />
          )}
        </div>
        <div className="space-y-2">
          <input
            ref={logoInputRef}
            type="file"
            accept="image/png,image/jpeg,image/webp,image/svg+xml"
            className="hidden"
            onChange={(e) => {
              const file = e.target.files?.[0];
              if (file) logoMutation.mutate(file);
            }}
          />
          <Button
            variant="secondary"
            size="sm"
            onClick={() => logoInputRef.current?.click()}
            loading={logoMutation.isPending}
          >
            Upload logo
          </Button>
          <p className="text-xs text-text-faint">PNG, JPG, WebP ili SVG, max 2 MB</p>
        </div>
      </div>

      <div className="grid sm:grid-cols-2 gap-4">
        <label className="block">
          <span className="text-xs font-medium text-text-muted mb-1 block">Primarna boja</span>
          <div className="flex items-center gap-2">
            <input
              type="color"
              value={primaryColor}
              onChange={(e) => setPrimaryColor(e.target.value)}
              className="w-11 h-11 rounded-lg border border-border cursor-pointer p-1"
            />
            <input
              type="text"
              value={primaryColor}
              onChange={(e) => setPrimaryColor(e.target.value)}
              className="input flex-1 font-mono text-sm"
            />
          </div>
        </label>
        <label className="block">
          <span className="text-xs font-medium text-text-muted mb-1 block">Akcentna boja</span>
          <div className="flex items-center gap-2">
            <input
              type="color"
              value={accentColor}
              onChange={(e) => setAccentColor(e.target.value)}
              className="w-11 h-11 rounded-lg border border-border cursor-pointer p-1"
            />
            <input
              type="text"
              value={accentColor}
              onChange={(e) => setAccentColor(e.target.value)}
              className="input flex-1 font-mono text-sm"
            />
          </div>
        </label>
      </div>

      <label className="flex items-center gap-3 cursor-pointer">
        <input
          type="checkbox"
          checked={onlineEnabled}
          onChange={(e) => setOnlineEnabled(e.target.checked)}
          disabled={!canBook}
          className="w-4 h-4 rounded border-border text-primary focus:ring-primary/30"
        />
        <span className="text-sm text-text">Omogući online rezervacije na javnoj stranici</span>
      </label>

      {branding.publicBookingUrl && (
        <div className="p-3 rounded-lg bg-surface-2 border border-border">
          <p className="text-xs text-text-muted mb-1">Javni link za klijente</p>
          <div className="flex flex-col sm:flex-row gap-2 sm:items-center">
            <code className="text-xs text-primary break-all flex-1">{branding.publicBookingUrl}</code>
            <Button variant="secondary" size="sm" icon={<Copy size={12} />} onClick={() => void copyLink()}>
              Kopiraj
            </Button>
          </div>
          <p className="text-[11px] text-text-faint mt-2">Slug salona: <strong>{branding.slug}</strong></p>
        </div>
      )}

      <div className="flex justify-end">
        <Button icon={<Save size={14} />} onClick={() => saveMutation.mutate()} loading={saveMutation.isPending}>
          Sačuvaj brending
        </Button>
      </div>
    </div>
  );
};
