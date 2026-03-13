namespace SalonPro.Application.Features.Settings.DTOs;

public record WorkingHoursDto(
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    bool IsWorkingDay
);
