namespace SalonPro.Infrastructure;

/// <summary>
/// Appointment start times are stored as salon-local wall clock
/// (no timezone offset), consistent with booking UI and <c>toLocalISOString</c> on the client.
/// Do not pass Unspecified times through <see cref="TimeZoneInfo.ConvertTimeFromUtc"/> — that treats them as UTC
/// and adds +1/+2h for Europe/Belgrade in the email/SMS text.
/// </summary>
internal static class AppointmentDateTimeHelper
{
    private static readonly TimeZoneInfo Belgrade = TimeZoneInfo.FindSystemTimeZoneById("Europe/Belgrade");

    /// <summary>Date/time as shown to clients in Serbia: convert only when the value is explicitly UTC.</summary>
    public static DateTime ToDisplayDateTime(DateTime startTime)
    {
        if (startTime.Kind == DateTimeKind.Utc)
            return TimeZoneInfo.ConvertTimeFromUtc(startTime, Belgrade);
        return startTime;
    }
}
