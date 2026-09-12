namespace SalonPro.Application.Common;

public static class SalonTimeZoneHelper
{
    public static TimeZoneInfo Resolve(string? timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
            catch { /* fallback */ }
        }

        try { return TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"); }
        catch { return TimeZoneInfo.Utc; }
    }
}
