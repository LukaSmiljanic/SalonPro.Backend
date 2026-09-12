import { useEffect, useMemo, useState } from 'react';
import type { CSSProperties, FormEvent } from 'react';
import { useParams } from 'react-router-dom';
import { fetchJson, postJson } from '../api';
import { resolveMediaUrl } from '../mediaUrl';

type Salon = {
  slug: string;
  name: string;
  logoUrl?: string | null;
  city?: string | null;
  phone?: string | null;
  address?: string | null;
  currency: string;
  primaryColor?: string | null;
  accentColor?: string | null;
  onlineBookingEnabled: boolean;
};

type Service = {
  id: string;
  name: string;
  durationMinutes: number;
  price: number;
};

type Staff = { id: string; fullName: string };

export function BookingPage() {
  const { slug } = useParams<{ slug: string }>();
  const [salon, setSalon] = useState<Salon | null>(null);
  const [services, setServices] = useState<Service[]>([]);
  const [staff, setStaff] = useState<Staff[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [doneId, setDoneId] = useState<string | null>(null);

  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [phone, setPhone] = useState('');
  const [email, setEmail] = useState('');
  const [staffId, setStaffId] = useState('');
  const [serviceId, setServiceId] = useState('');
  const [startTime, setStartTime] = useState('');
  const [notes, setNotes] = useState('');

  const themeStyle = useMemo<CSSProperties>(() => ({
    ['--brand-primary' as string]: salon?.primaryColor ?? '#5b3a8c',
    ['--brand-accent' as string]: salon?.accentColor ?? '#8b5cf6',
  }), [salon?.primaryColor, salon?.accentColor]);

  useEffect(() => {
    if (!slug) return;
    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const s = await fetchJson<Salon>(`/api/public/booking/${encodeURIComponent(slug)}`);
        if (cancelled) return;
        setSalon(s);

        if (!s.onlineBookingEnabled) {
          setError('Online zakazivanje trenutno nije dostupno za ovaj salon.');
          return;
        }

        const [sv, st] = await Promise.all([
          fetchJson<Service[]>(`/api/public/booking/${encodeURIComponent(slug)}/services`),
          fetchJson<Staff[]>(`/api/public/booking/${encodeURIComponent(slug)}/staff`),
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
    return () => {
      cancelled = true;
    };
  }, [slug]);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    if (!slug || !staffId || !serviceId || !startTime) return;
    setSubmitting(true);
    setError(null);
    try {
      const id = await postJson<string>(`/api/public/booking/${encodeURIComponent(slug)}/appointments`, {
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        phone: phone.trim(),
        email: email.trim() || null,
        staffMemberId: staffId,
        serviceIds: [serviceId],
        startTime: new Date(startTime).toISOString(),
        notes: notes.trim() || null,
      });
      setDoneId(id);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Slanje nije uspelo.');
    } finally {
      setSubmitting(false);
    }
  }

  if (!slug) return null;

  if (loading) {
    return (
      <div className="page" style={themeStyle}>
        <p className="muted center">Učitavanje…</p>
      </div>
    );
  }

  if (error && !salon) {
    return (
      <div className="page" style={themeStyle}>
        <div className="card">
          <p className="error">{error}</p>
          <p className="muted small">Proverite link ili kontaktirajte salon.</p>
        </div>
      </div>
    );
  }

  if (!salon) return null;

  const logoSrc = resolveMediaUrl(salon.logoUrl);

  if (doneId) {
    return (
      <div className="page" style={themeStyle}>
        <div className="card success-card">
          <SalonHeader salon={salon} logoSrc={logoSrc} />
          <p className="success-text">Hvala — zahtev za termin je poslat. Očekujte potvrdu od salona.</p>
        </div>
      </div>
    );
  }

  if (!salon.onlineBookingEnabled) {
    return (
      <div className="page" style={themeStyle}>
        <div className="card">
          <SalonHeader salon={salon} logoSrc={logoSrc} />
          <p className="muted">Online zakazivanje trenutno nije aktivno.</p>
          {salon.phone && (
            <p className="small">
              Pozovite nas: <a href={`tel:${salon.phone}`}>{salon.phone}</a>
            </p>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className="page" style={themeStyle}>
      <div className="card">
        <SalonHeader salon={salon} logoSrc={logoSrc} />

        <form onSubmit={onSubmit} className="form">
          <label>
            Ime
            <input required value={firstName} onChange={e => setFirstName(e.target.value)} />
          </label>
          <label>
            Prezime
            <input required value={lastName} onChange={e => setLastName(e.target.value)} />
          </label>
          <label>
            Telefon
            <input required type="tel" value={phone} onChange={e => setPhone(e.target.value)} />
          </label>
          <label>
            Email (opciono)
            <input type="email" value={email} onChange={e => setEmail(e.target.value)} />
          </label>

          <label>
            Zaposleni
            <select required value={staffId} onChange={e => setStaffId(e.target.value)}>
              <option value="">— izaberite —</option>
              {staff.map(s => (
                <option key={s.id} value={s.id}>{s.fullName}</option>
              ))}
            </select>
          </label>

          <label>
            Usluga
            <select required value={serviceId} onChange={e => setServiceId(e.target.value)}>
              <option value="">— izaberite —</option>
              {services.map(s => (
                <option key={s.id} value={s.id}>
                  {s.name} ({s.durationMinutes} min, {s.price} {salon.currency})
                </option>
              ))}
            </select>
          </label>

          <label>
            Datum i vreme početka
            <input
              required
              type="datetime-local"
              value={startTime}
              onChange={e => setStartTime(e.target.value)}
            />
          </label>

          <label>
            Napomena (opciono)
            <textarea value={notes} onChange={e => setNotes(e.target.value)} rows={3} />
          </label>

          {error && <p className="error small">{error}</p>}

          <button type="submit" disabled={submitting} className="submit-btn">
            {submitting ? 'Šaljem…' : 'Pošalji zahtev'}
          </button>
        </form>
      </div>

      <p className="footer-note">Powered by SalonPro</p>
    </div>
  );
}

function SalonHeader({ salon, logoSrc }: { salon: Salon; logoSrc: string | null }) {
  return (
    <header className="salon-header">
      {logoSrc && (
        <img src={logoSrc} alt="" className="salon-logo" />
      )}
      <div>
        <h1>{salon.name}</h1>
        {salon.address && <p className="muted small">{salon.address}</p>}
        {salon.city && <p className="muted small">{salon.city}</p>}
        {salon.phone && (
          <p className="small">
            Tel: <a href={`tel:${salon.phone}`}>{salon.phone}</a>
          </p>
        )}
      </div>
    </header>
  );
}
