using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SalonPro.Application.Common.Interfaces;

namespace SalonPro.Infrastructure.Email;

public class MailKitEmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<MailKitEmailService> _logger;

    public MailKitEmailService(IOptions<SmtpSettings> settings, ILogger<MailKitEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAppointmentConfirmationAsync(AppointmentEmailDto dto, CancellationToken cancellationToken = default)
    {
        var (dateFormatted, timeFormatted, price) = FormatDetails(dto);

        var html = EmailTemplates.AppointmentConfirmation(
            dto.ClientName, dto.SalonName, dateFormatted, timeFormatted,
            dto.ServiceNames, dto.StaffName, dto.DurationMinutes, price,
            dto.SalonPhone, dto.SalonAddress);

        await SendEmailAsync(dto.ClientEmail, $"Potvrda termina — {dto.SalonName}", html, cancellationToken);
    }

    public async Task SendAppointmentReminderAsync(AppointmentEmailDto dto, CancellationToken cancellationToken = default)
    {
        var (dateFormatted, timeFormatted, price) = FormatDetails(dto);

        var html = EmailTemplates.AppointmentReminder(
            dto.ClientName, dto.SalonName, dateFormatted, timeFormatted,
            dto.ServiceNames, dto.StaffName, dto.DurationMinutes, price,
            dto.SalonPhone, dto.SalonAddress);

        await SendEmailAsync(dto.ClientEmail, $"Podsetnik za sutrašnji termin — {dto.SalonName}", html, cancellationToken);
    }

    public async Task SendAppointmentCancellationAsync(AppointmentEmailDto dto, string? reason, CancellationToken cancellationToken = default)
    {
        var (dateFormatted, timeFormatted, _) = FormatDetails(dto);

        var html = EmailTemplates.AppointmentCancellation(
            dto.ClientName, dto.SalonName, dateFormatted, timeFormatted,
            dto.ServiceNames, dto.StaffName, reason);

        await SendEmailAsync(dto.ClientEmail, $"Termin otkazan — {dto.SalonName}", html, cancellationToken);
    }

    public async Task SendEmailVerificationAsync(string toEmail, string tenantName, string verificationUrl, CancellationToken cancellationToken = default)
    {
        var html = EmailTemplates.EmailVerification(tenantName, verificationUrl);
        await SendEmailAsync(toEmail, "Aktivirajte Vaš SalonPro nalog", html, cancellationToken);
    }

    public async Task SendSubscriptionWarningAsync(string toEmail, string tenantName, int daysRemaining, DateTime expirationDate, CancellationToken cancellationToken = default)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Belgrade");
        var localDate = TimeZoneInfo.ConvertTimeFromUtc(expirationDate, tz);
        var html = EmailTemplates.SubscriptionWarning(tenantName, daysRemaining, localDate.ToString("dd.MM.yyyy"));
        await SendEmailAsync(toEmail, $"Pretplata ističe za {daysRemaining} dana — {tenantName}", html, cancellationToken);
    }

    public async Task SendSubscriptionExpiredAsync(string toEmail, string tenantName, CancellationToken cancellationToken = default)
    {
        var html = EmailTemplates.SubscriptionExpired(tenantName);
        await SendEmailAsync(toEmail, $"Pretplata je istekla — {tenantName}", html, cancellationToken);
    }

    public async Task SendPasswordResetAsync(string toEmail, string resetUrl, CancellationToken cancellationToken = default)
    {
        var html = EmailTemplates.PasswordReset(resetUrl);
        await SendEmailAsync(toEmail, "Resetovanje lozinke — SalonPro", html, cancellationToken);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Email sending is disabled. Skipping email to {Email}: {Subject}", toEmail, subject);
            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.Host) || string.IsNullOrWhiteSpace(_settings.Username))
        {
            _logger.LogWarning("SMTP not configured. Skipping email to {Email}: {Subject}", toEmail, subject);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Port 465 = implicit SSL, Port 587 = STARTTLS
            var secureSocketOptions = _settings.Port == 465
                ? SecureSocketOptions.SslOnConnect
                : _settings.UseSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.Auto;

            await client.ConnectAsync(_settings.Host, _settings.Port, secureSocketOptions, cancellationToken);
            await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}: {Subject}", toEmail, subject);
            // Don't throw — email failure should not break the appointment flow
        }
    }

    private static (string Date, string Time, string Price) FormatDetails(AppointmentEmailDto dto)
    {
        // Belgrade timezone for display
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Belgrade");
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(dto.StartTime, tz);

        var dateFormatted = localTime.ToString("dd.MM.yyyy");
        var timeFormatted = localTime.ToString("HH:mm");
        var price = $"{dto.TotalPrice:N0} {dto.Currency}";

        return (dateFormatted, timeFormatted, price);
    }
}
