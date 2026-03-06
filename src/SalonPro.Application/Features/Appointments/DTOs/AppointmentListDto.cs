using SalonPro.Domain.Enums;

namespace SalonPro.Application.Features.Appointments.DTOs;

/// <summary>List-view appointment (e.g. in staff schedule or by date).</summary>
public record AppointmentListDto(
    Guid Id,
    string ClientName,
    string StaffMemberName,
    string ServiceNames,
    DateTime StartTime,
    DateTime EndTime,
    AppointmentStatus Status,
    decimal TotalPrice,
    string? CategoryColorHex
);
