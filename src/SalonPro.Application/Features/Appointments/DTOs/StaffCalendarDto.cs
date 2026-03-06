namespace SalonPro.Application.Features.Appointments.DTOs;

public record StaffCalendarDto(
    Guid StaffMemberId,
    string StaffMemberName,
    int AppointmentCount,
    List<CalendarAppointmentDto> Appointments
);
