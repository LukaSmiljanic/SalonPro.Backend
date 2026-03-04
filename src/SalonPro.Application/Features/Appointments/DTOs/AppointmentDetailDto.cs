using SalonPro.Domain.Enums;

namespace SalonPro.Application.Features.Appointments.DTOs;

public record AppointmentDetailDto(
    Guid Id,
    Guid ClientId,
    string ClientName,
    Guid StaffMemberId,
    string StaffMemberName,
    DateTime StartTime,
    DateTime EndTime,
    AppointmentStatus Status,
    decimal TotalPrice,
    string? Notes,
    string? CancellationReason,
    List<AppointmentServiceDto> Services
);
