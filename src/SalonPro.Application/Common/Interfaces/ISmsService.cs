namespace SalonPro.Application.Common.Interfaces;

public interface ISmsService
{
    Task SendAppointmentConfirmationAsync(AppointmentSmsDto dto, CancellationToken cancellationToken = default);
    Task SendAppointmentReminderAsync(AppointmentSmsDto dto, CancellationToken cancellationToken = default);
    Task SendAppointmentCancellationAsync(AppointmentSmsDto dto, string? reason, CancellationToken cancellationToken = default);
}

public record AppointmentSmsDto(
    string ClientName,
    string ClientPhone,
    string SalonName,
    string StaffName,
    DateTime StartTime,
    int DurationMinutes,
    string ServiceNames,
    decimal TotalPrice,
    string Currency,
    string? SalonPhone
);
