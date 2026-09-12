const API = '/api';

async function fetchPublic<T>(path: string): Promise<T> {
  const res = await fetch(`${API}${path}`);
  if (!res.ok) {
    let detail = res.statusText;
    try {
      const j = (await res.json()) as { detail?: string; title?: string };
      detail = j.detail || j.title || detail;
    } catch {
      /* ignore */
    }
    throw new Error(detail);
  }
  return res.json() as Promise<T>;
}

async function postPublic<T>(path: string, body: unknown): Promise<T> {
  const res = await fetch(`${API}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    let detail = res.statusText;
    try {
      const j = (await res.json()) as { detail?: string; title?: string };
      detail = j.detail || j.title || detail;
    } catch {
      /* ignore */
    }
    throw new Error(detail);
  }
  return res.json() as Promise<T>;
}

export interface PublicBookingSalon {
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
}

export interface PublicBookingService {
  id: string;
  name: string;
  durationMinutes: number;
  price: number;
}

export interface PublicBookingStaff {
  id: string;
  fullName: string;
}

export const getPublicSalon = (slug: string) =>
  fetchPublic<PublicBookingSalon>(`/public/booking/${encodeURIComponent(slug)}`);

export const getPublicServices = (slug: string) =>
  fetchPublic<PublicBookingService[]>(`/public/booking/${encodeURIComponent(slug)}/services`);

export const getPublicStaff = (slug: string) =>
  fetchPublic<PublicBookingStaff[]>(`/public/booking/${encodeURIComponent(slug)}/staff`);

export const createPublicBooking = (
  slug: string,
  body: {
    firstName: string;
    lastName: string;
    phone: string;
    email: string | null;
    staffMemberId: string;
    serviceIds: string[];
    startTime: string;
    notes: string | null;
  },
) => postPublic<string>(`/public/booking/${encodeURIComponent(slug)}/appointments`, body);
