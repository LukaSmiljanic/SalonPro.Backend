using SalonPro.Application.Features.Appointments.DTOs;

namespace SalonPro.Application.Features.Staff.DTOs;

public record WorkingHoursDto(
    Guid Id,
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    bool IsWorkingDay
);

public record StaffScheduleDto(
    Guid StaffMemberId,
    string FullName,
    string? Specialization,
    string? AvatarUrl,
    List<WorkingHoursDto> WorkingHours,
    List<AppointmentDto> Appointments
);
