namespace SalonPro.Application.Features.Settings.DTOs;

public record TenantWorkingHoursDto(
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    bool IsWorkingDay
);
