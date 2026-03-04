namespace SalonPro.Application.Features.Appointments.DTOs;

public record CalendarDayDto(
    DateTime Date,
    List<AppointmentDto> Appointments
);
