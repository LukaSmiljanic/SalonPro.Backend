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
