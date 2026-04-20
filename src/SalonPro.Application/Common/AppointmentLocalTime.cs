namespace SalonPro.Application.Common;

/// <summary>
/// Appointment <see cref="Domain.Entities.Appointment.StartTime"/> / EndTime are stored as naive wall-clock
/// values in the salon timezone (same as booking UI). Comparing them directly to <see cref="DateTime.UtcNow"/>
/// in commands is incorrect (off by offset).
/// </summary>
public static class AppointmentLocalTime
{
    private static readonly TimeZoneInfo Belgrade = TimeZoneInfo.FindSystemTimeZoneById("Europe/Belgrade");

    /// <summary>Converts a stored end time to UTC for comparison with <paramref name="utcNow"/>.</summary>
    public static DateTime EndTimeToUtc(DateTime endTime)
    {
        return endTime.Kind switch
        {
            DateTimeKind.Utc => endTime,
            DateTimeKind.Local => endTime.ToUniversalTime(),
            _ => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(endTime, DateTimeKind.Unspecified), Belgrade),
        };
    }

    /// <summary>True if the appointment slot has ended in real time (end ≤ now in UTC).</summary>
    public static bool IsEnded(DateTime endTime, DateTime utcNow) =>
        EndTimeToUtc(endTime) <= utcNow;
}
