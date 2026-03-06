namespace SalonPro.Application.Features.Appointments.DTOs;

public record WeeklyCalendarDto(
    DateTime StartDate,
    DateTime EndDate,
    List<StaffCalendarDto> StaffSchedules
);
