export function apiBase(): string {
  const b = import.meta.env.VITE_API_BASE_URL;
  return typeof b === 'string' ? b.replace(/\/$/, '') : '';
}

export function resolveMediaUrl(url: string | null | undefined): string | null {
  if (!url?.trim()) return null;
  const trimmed = url.trim();

  if (trimmed.startsWith('http://') || trimmed.startsWith('https://')) return trimmed;

  if (trimmed.startsWith('/media/')) {
    const base = apiBase();
    return base ? `${base}${trimmed}` : trimmed;
  }

  const path = trimmed.startsWith('/') ? trimmed : `/media/social/${trimmed}`;
  const base = apiBase();
  return base ? `${base}${path}` : path;
}
