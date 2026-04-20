/** Bazni URL API-ja; prazno = relativne putanje (Vite proxy u dev-u). */
export function apiBase(): string {
  const b = import.meta.env.VITE_API_BASE_URL;
  return typeof b === 'string' ? b.replace(/\/$/, '') : '';
}

export async function fetchJson<T>(path: string): Promise<T> {
  const url = `${apiBase()}${path}`;
  const res = await fetch(url);
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || res.statusText);
  }
  return res.json() as Promise<T>;
}

export async function postJson<T>(path: string, body: unknown): Promise<T> {
  const url = `${apiBase()}${path}`;
  const res = await fetch(url, {
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
