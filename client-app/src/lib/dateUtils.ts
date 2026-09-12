import { format, isValid, parse } from 'date-fns';

export const ISO_DATE_FORMAT = 'yyyy-MM-dd';
export const DISPLAY_DATE_FORMAT = 'dd.MM.yyyy';

/** ISO date (yyyy-MM-dd) → European display (dd.MM.yyyy). */
export function isoDateToDisplay(iso: string): string {
  if (!iso) return '';
  const parsed = parse(iso, ISO_DATE_FORMAT, new Date());
  return isValid(parsed) ? format(parsed, DISPLAY_DATE_FORMAT) : '';
}

/** European display (dd.MM.yyyy) → ISO date (yyyy-MM-dd), or null if invalid. */
export function displayDateToIso(display: string): string | null {
  const trimmed = display.trim();
  if (!trimmed) return null;
  const parsed = parse(trimmed, DISPLAY_DATE_FORMAT, new Date());
  if (!isValid(parsed)) return null;
  return format(parsed, ISO_DATE_FORMAT);
}

/**
 * Formats a Date as an ISO-like string in the LOCAL timezone,
 * e.g. "2026-03-24T17:00:00" instead of the UTC-shifted "2026-03-24T15:00:00Z"
 * that Date.toISOString() would produce.
 *
 * This is needed because the backend stores appointment times as naive
 * datetime values (no timezone offset) and returns them as-is.
 * If we send UTC-converted strings the frontend will misinterpret them
 * when they come back without the "Z" suffix.
 */
export function toLocalISOString(date: Date): string {
  const pad = (n: number) => n.toString().padStart(2, '0');
  return (
    `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}` +
    `T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
  );
}
