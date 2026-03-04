using SalonPro.Domain.Enums;

namespace SalonPro.Application.Features.Appointments.DTOs;

public record AppointmentServiceDto(
    Guid ServiceId,
    string ServiceName,
    decimal Price,
    int DurationMinutes
);

public record AppointmentDto(
    Guid Id,
    string ClientName,
    string StaffMemberName,
    DateTime StartTime,
    DateTime EndTime,
    AppointmentStatus Status,
    decimal TotalPrice,
    string? Notes,
    List<AppointmentServiceDto> Services
);
