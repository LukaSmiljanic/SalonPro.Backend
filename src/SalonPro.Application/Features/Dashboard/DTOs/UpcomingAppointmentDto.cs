namespace SalonPro.Application.Features.Dashboard.DTOs;

public record UpcomingAppointmentDto(
    Guid Id,
    string ClientName,
    string ServiceName,
    string StaffName,
    DateTime StartTime,
    string Status
);
