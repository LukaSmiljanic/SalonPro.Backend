import React, { useEffect, useMemo, useState } from 'react';
import type { CSSProperties, FormEvent } from 'react';
import { useParams } from 'react-router-dom';
import {
  createPublicBooking,
  getPublicSalon,
  getPublicServices,
  getPublicStaff,
  type PublicBookingSalon,
} from '../api/publicBooking';
import { resolveSocialImageUrl } from '../lib/mediaUrl';
import { LoadingSpinner } from '../components/LoadingSpinner';

export const PublicBookingPage: React.FC = () => {
  const { slug } = useParams<{ slug: string }>();
  const [salon, setSalon] = useState<PublicBookingSalon | null>(null);
  const [services, setServices] = useState<{ id: string; name: string; durationMinutes: number; price: number }[]>([]);
  const [staff, setStaff] = useState<{ id: string; fullName: string }[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [done, setDone] = useState(false);

  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [phone, setPhone] = useState('');
  const [email, setEmail] = useState('');
  const [staffId, setStaffId] = useState('');
  const [serviceId, setServiceId] = useState('');
  const [startTime, setStartTime] = useState('');
  const [notes, setNotes] = useState('');

  const themeStyle = useMemo<CSSProperties>(() => ({
    ['--book-primary' as string]: salon?.primaryColor ?? '#5b3a8c',
    ['--book-accent' as string]: salon?.accentColor ?? '#8b5cf6',
  }), [salon?.primaryColor, salon?.accentColor]);

  useEffect(() => {
    if (!slug) return;
    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const s = await getPublicSalon(slug);
        if (cancelled) return;
        setSalon(s);

        if (!s.onlineBookingEnabled) {
          setError('Online zakazivanje trenutno nije dostupno za ovaj salon.');
          return;
        }

        const [sv, st] = await Promise.all([
          getPublicServices(slug),
          getPublicStaff(slug),
        ]);
        if (cancelled) return;
        setServices(Array.isArray(sv) ? sv : []);
        setStaff(Array.isArray(st) ? st : []);
        if (st.length === 1) setStaffId(st[0].id);
        if (sv.length === 1) setServiceId(sv[0].id);
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : 'Greška pri učitavanju.');
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => { cancelled = true; };
  }, [slug]);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    if (!slug || !staffId || !serviceId || !startTime) return;
    setSubmitting(true);
    setError(null);
    try {
      await createPublicBooking(slug, {
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        phone: phone.trim(),
        email: email.trim() || null,
        staffMemberId: staffId,
        serviceIds: [serviceId],
        startTime: new Date(startTime).toISOString(),
        notes: notes.trim() || null,
      });
      setDone(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Slanje nije uspelo.');
    } finally {
      setSubmitting(false);
    }
  }

  if (!slug) return null;

  const logoSrc = resolveSocialImageUrl(salon?.logoUrl);

  return (
    <div
      className="min-h-screen bg-[#f6f4fa] py-8 px-4 flex flex-col items-center"
      style={themeStyle}
    >
      {loading ? (
        <div className="py-16">
          <LoadingSpinner />
        </div>
      ) : error && !salon ? (
        <div className="w-full max-w-md bg-white rounded-2xl border border-border p-6 shadow-sm">
          <p className="text-error text-sm">{error}</p>
          <p className="text-text-muted text-xs mt-2">Proverite link ili kontaktirajte salon.</p>
        </div>
      ) : salon ? (
        <div className="w-full max-w-md bg-white rounded-2xl border border-border p-5 sm:p-6 shadow-sm">
          <SalonHeader salon={salon} logoSrc={logoSrc} />

          {done ? (
            <p className="text-success text-sm font-medium mt-4">
              Hvala — zahtev za termin je poslat. Očekujte potvrdu od salona.
            </p>
          ) : !salon.onlineBookingEnabled ? (
            <div className="mt-4 space-y-2">
              <p className="text-text-muted text-sm">Online zakazivanje trenutno nije aktivno.</p>
              {salon.phone && (
                <p className="text-sm">
                  Pozovite nas:{' '}
                  <a href={`tel:${salon.phone}`} className="text-[var(--book-primary)] font-medium">
                    {salon.phone}
                  </a>
                </p>
              )}
            </div>
          ) : (
            <form onSubmit={onSubmit} className="mt-5 space-y-3">
              <Field label="Ime">
                <input required value={firstName} onChange={e => setFirstName(e.target.value)} className="book-input" />
              </Field>
              <Field label="Prezime">
                <input required value={lastName} onChange={e => setLastName(e.target.value)} className="book-input" />
              </Field>
              <Field label="Telefon">
                <input required type="tel" value={phone} onChange={e => setPhone(e.target.value)} className="book-input" />
              </Field>
              <Field label="Email (opciono)">
                <input type="email" value={email} onChange={e => setEmail(e.target.value)} className="book-input" />
              </Field>
              <Field label="Zaposleni">
                <select required value={staffId} onChange={e => setStaffId(e.target.value)} className="book-input">
                  <option value="">— izaberite —</option>
                  {staff.map(s => (
                    <option key={s.id} value={s.id}>{s.fullName}</option>
                  ))}
                </select>
              </Field>
              <Field label="Usluga">
                <select required value={serviceId} onChange={e => setServiceId(e.target.value)} className="book-input">
                  <option value="">— izaberite —</option>
                  {services.map(s => (
                    <option key={s.id} value={s.id}>
                      {s.name} ({s.durationMinutes} min, {s.price} {salon.currency})
                    </option>
                  ))}
                </select>
              </Field>
              <Field label="Datum i vreme početka">
                <input
                  required
                  type="datetime-local"
                  value={startTime}
                  onChange={e => setStartTime(e.target.value)}
                  className="book-input"
                />
              </Field>
              <Field label="Napomena (opciono)">
                <textarea value={notes} onChange={e => setNotes(e.target.value)} rows={3} className="book-input resize-y" />
              </Field>

              {error && <p className="text-error text-xs">{error}</p>}

              <button
                type="submit"
                disabled={submitting}
                className="w-full mt-2 py-3 rounded-xl text-white font-semibold text-sm disabled:opacity-70
                  bg-gradient-to-r from-[var(--book-primary)] to-[var(--book-accent)]"
              >
                {submitting ? 'Šaljem…' : 'Pošalji zahtev'}
              </button>
            </form>
          )}
        </div>
      ) : null}

      <p className="text-[11px] text-text-faint mt-4">Powered by SalonPro</p>
    </div>
  );
};

function SalonHeader({ salon, logoSrc }: { salon: PublicBookingSalon; logoSrc: string | null }) {
  return (
    <header className="flex gap-4 items-center pb-4 border-b border-border">
      {logoSrc && (
        <img
          src={logoSrc}
          alt=""
          className="w-16 h-16 rounded-xl object-contain border border-border bg-surface-2 p-1 shrink-0"
        />
      )}
      <div className="min-w-0">
        <h1 className="text-lg font-semibold text-[var(--book-primary)] truncate">{salon.name}</h1>
        {salon.address && <p className="text-xs text-text-muted truncate">{salon.address}</p>}
        {salon.city && <p className="text-xs text-text-muted">{salon.city}</p>}
        {salon.phone && (
          <p className="text-xs mt-1">
            Tel:{' '}
            <a href={`tel:${salon.phone}`} className="text-[var(--book-primary)]">
              {salon.phone}
            </a>
          </p>
        )}
      </div>
    </header>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <label className="block">
      <span className="text-xs font-medium text-text-muted mb-1 block">{label}</span>
      {children}
    </label>
  );
}
