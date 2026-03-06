using SalonPro.Domain.Enums;

namespace SalonPro.Application.Features.Appointments.DTOs;

public record CalendarAppointmentDto(
    Guid Id,
    string ClientName,
    string ServiceName,
    DateTime StartTime,
    DateTime EndTime,
    ServiceCategoryType CategoryType,
    string? ColorHex
);
