/** Relative path for Netlify proxy → Monster (same as /api/*). */
export function toProxiedMediaPath(url: string): string | null {
  const mediaMatch = url.match(/\/media\/social\/(.+)$/i);
  if (mediaMatch) return `/media/social/${mediaMatch[1]}`;

  const trimmed = url.trim();
  if (!trimmed.startsWith('/') && !trimmed.startsWith('http')) {
    return `/media/social/${trimmed}`;
  }
  if (trimmed.startsWith('/media/social/')) return trimmed;
  return null;
}

/** API origin without /api suffix — local dev fallback only. */
export function getMediaOrigin(): string {
  const configured = (import.meta as ImportMeta & { env: { VITE_API_URL?: string } }).env.VITE_API_URL;
  if (configured?.trim()) {
    return configured.trim().replace(/\/api\/?$/i, '');
  }
  return '';
}

/**
 * Browser URL for social images.
 * On Netlify: always /media/social/... (proxied to Monster via _redirects).
 * Local dev: prepend API host when needed.
 */
export function resolveSocialImageUrl(url: string | null | undefined): string | null {
  if (!url?.trim()) return null;

  const proxied = toProxiedMediaPath(url.trim());
  if (proxied) return proxied;

  const trimmed = url.trim();
  if (/^https?:\/\//i.test(trimmed)) {
    // Last resort — direct API host (may fail if TLS/hotlink blocked)
    return trimmed;
  }

  const origin = getMediaOrigin();
  if (origin) {
    return trimmed.startsWith('/') ? `${origin}${trimmed}` : `${origin}/media/social/${trimmed}`;
  }

  return trimmed.startsWith('/') ? trimmed : `/media/social/${trimmed}`;
}
