namespace SalonPro.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendAppointmentConfirmationAsync(AppointmentEmailDto appointment, CancellationToken cancellationToken = default);
    Task SendAppointmentReminderAsync(AppointmentEmailDto appointment, CancellationToken cancellationToken = default);
    Task SendAppointmentCancellationAsync(AppointmentEmailDto appointment, string? reason, CancellationToken cancellationToken = default);
    Task SendEmailVerificationAsync(string toEmail, string tenantName, string verificationUrl, CancellationToken cancellationToken = default);
}

public record AppointmentEmailDto(
    string ClientName,
    string ClientEmail,
    string SalonName,
    string StaffName,
    DateTime StartTime,
    int DurationMinutes,
    string ServiceNames,
    decimal TotalPrice,
    string Currency,
    string? SalonPhone,
    string? SalonAddress
);
