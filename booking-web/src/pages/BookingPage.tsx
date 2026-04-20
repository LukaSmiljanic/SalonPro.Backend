import { useEffect, useState } from 'react';
import type { CSSProperties } from 'react';
import { useParams } from 'react-router-dom';
import { fetchJson, postJson } from '../api';

type Salon = {
  slug: string;
  name: string;
  logoUrl?: string | null;
  city?: string | null;
  phone?: string | null;
  address?: string | null;
  currency: string;
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

  useEffect(() => {
    if (!slug) return;
    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const [s, sv, st] = await Promise.all([
          fetchJson<Salon>(`/api/public/booking/${encodeURIComponent(slug)}`),
          fetchJson<Service[]>(`/api/public/booking/${encodeURIComponent(slug)}/services`),
          fetchJson<Staff[]>(`/api/public/booking/${encodeURIComponent(slug)}/staff`),
        ]);
        if (cancelled) return;
        setSalon(s);
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

  async function onSubmit(e: React.FormEvent) {
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
      <div style={{ padding: '2rem', textAlign: 'center' }}>
        Učitavanje…
      </div>
    );
  }

  if (error && !salon) {
    return (
      <div style={{ maxWidth: 480, margin: '2rem auto', padding: '0 1rem' }}>
        <p style={{ color: '#b00020' }}>{error}</p>
        <p style={{ color: '#666', fontSize: '0.9rem' }}>
          Proverite link ili kontaktirajte salon.
        </p>
      </div>
    );
  }

  if (doneId && salon) {
    return (
      <div style={{ maxWidth: 480, margin: '2rem auto', padding: '0 1rem' }}>
        <h1 style={{ fontSize: '1.25rem' }}>{salon.name}</h1>
        <p>Hvala — termin je prijavljen. Možete očekivati potvrdu od salona.</p>
      </div>
    );
  }

  if (!salon) return null;

  return (
    <div style={{ maxWidth: 480, margin: '2rem auto', padding: '0 1rem' }}>
      <header style={{ marginBottom: '1.5rem' }}>
        <h1 style={{ fontSize: '1.35rem', margin: '0 0 0.25rem' }}>{salon.name}</h1>
        {salon.address && <p style={{ margin: 0, color: '#555', fontSize: '0.9rem' }}>{salon.address}</p>}
        {salon.city && <p style={{ margin: 0, color: '#555', fontSize: '0.9rem' }}>{salon.city}</p>}
        {salon.phone && (
          <p style={{ margin: '0.5rem 0 0', fontSize: '0.9rem' }}>
            Tel: <a href={`tel:${salon.phone}`}>{salon.phone}</a>
          </p>
        )}
      </header>

      <form onSubmit={onSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
        <label>
          Ime
          <input
            required
            value={firstName}
            onChange={e => setFirstName(e.target.value)}
            style={inputStyle}
          />
        </label>
        <label>
          Prezime
          <input
            required
            value={lastName}
            onChange={e => setLastName(e.target.value)}
            style={inputStyle}
          />
        </label>
        <label>
          Telefon
          <input
            required
            type="tel"
            value={phone}
            onChange={e => setPhone(e.target.value)}
            style={inputStyle}
          />
        </label>
        <label>
          Email (opciono)
          <input
            type="email"
            value={email}
            onChange={e => setEmail(e.target.value)}
            style={inputStyle}
          />
        </label>

        <label>
          Zaposleni
          <select required value={staffId} onChange={e => setStaffId(e.target.value)} style={inputStyle}>
            <option value="">— izaberite —</option>
            {staff.map(s => (
              <option key={s.id} value={s.id}>
                {s.fullName}
              </option>
            ))}
          </select>
        </label>

        <label>
          Usluga
          <select required value={serviceId} onChange={e => setServiceId(e.target.value)} style={inputStyle}>
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
            style={inputStyle}
          />
        </label>

        <label>
          Napomena (opciono)
          <textarea value={notes} onChange={e => setNotes(e.target.value)} rows={3} style={inputStyle} />
        </label>

        {error && <p style={{ color: '#b00020', margin: 0, fontSize: '0.9rem' }}>{error}</p>}

        <button
          type="submit"
          disabled={submitting}
          style={{
            marginTop: '0.5rem',
            padding: '0.65rem 1rem',
            fontWeight: 600,
            background: '#5b3a8c',
            color: '#fff',
            border: 'none',
            borderRadius: 8,
            cursor: submitting ? 'wait' : 'pointer',
          }}
        >
          {submitting ? 'Šaljem…' : 'Pošalji zahtev'}
        </button>
      </form>
    </div>
  );
}

const inputStyle: CSSProperties = {
  display: 'block',
  width: '100%',
  marginTop: 4,
  padding: '0.5rem 0.6rem',
  borderRadius: 6,
  border: '1px solid #ccc',
  font: 'inherit',
};
