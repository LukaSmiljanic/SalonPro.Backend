export type ThemeMode = 'light' | 'dark';

const PREFS_KEY = 'salonpro-theme-prefs';
const GUEST_KEY = 'guest';

export function applyTheme(mode: ThemeMode): void {
  const root = document.documentElement;
  if (mode === 'dark') {
    root.setAttribute('data-theme', 'dark');
  } else {
    root.removeAttribute('data-theme');
  }
}

function readPrefs(): Record<string, ThemeMode> {
  try {
    const raw = localStorage.getItem(PREFS_KEY);
    if (!raw) return {};
    const parsed = JSON.parse(raw) as Record<string, string>;
    const out: Record<string, ThemeMode> = {};
    for (const [k, v] of Object.entries(parsed)) {
      if (v === 'dark' || v === 'light') out[k] = v;
    }
    return out;
  } catch {
    return {};
  }
}

function writePrefs(prefs: Record<string, ThemeMode>): void {
  localStorage.setItem(PREFS_KEY, JSON.stringify(prefs));
}

export function getStoredUserIdFromToken(): string | null {
  try {
    const token = localStorage.getItem('salonpro_token');
    if (!token) return null;
    const base64 = token.split('.')[1]?.replace(/-/g, '+').replace(/_/g, '/');
    if (!base64) return null;
    const payload = JSON.parse(atob(base64)) as Record<string, unknown>;
    const id =
      payload.sub ??
      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
    return typeof id === 'string' && id.length > 0 ? id : null;
  } catch {
    return null;
  }
}

export function storageKeyForUser(userId: string | null | undefined): string {
  return userId?.trim() ? userId.trim() : GUEST_KEY;
}

export function getThemeForUser(userId: string | null | undefined): ThemeMode {
  const key = storageKeyForUser(userId);
  const prefs = readPrefs();
  if (prefs[key]) return prefs[key];
  if (typeof window !== 'undefined' && window.matchMedia('(prefers-color-scheme: dark)').matches) {
    return 'dark';
  }
  return 'light';
}

export function setThemeForUser(userId: string | null | undefined, mode: ThemeMode): void {
  const key = storageKeyForUser(userId);
  const prefs = readPrefs();
  prefs[key] = mode;
  writePrefs(prefs);
  applyTheme(mode);
}

/** Apply saved theme before React mounts (login page / refresh). */
export function initThemeFromStorage(): ThemeMode {
  const userId = getStoredUserIdFromToken();
  const mode = getThemeForUser(userId);
  applyTheme(mode);
  return mode;
}
