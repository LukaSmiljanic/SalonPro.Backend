namespace SalonPro.Application.Features.Appointments.DTOs;

/// <summary>Detail of a service within an appointment (same shape as AppointmentServiceDto).</summary>
public record AppointmentServiceDetailDto(
    Guid ServiceId,
    string ServiceName,
    decimal Price,
    int DurationMinutes
);
