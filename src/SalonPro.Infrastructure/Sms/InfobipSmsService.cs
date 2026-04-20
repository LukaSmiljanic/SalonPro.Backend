using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SalonPro.Application.Common.Interfaces;

namespace SalonPro.Infrastructure.Sms;

public class InfobipSmsService : ISmsService
{
    private readonly SmsSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<InfobipSmsService> _logger;

    public InfobipSmsService(
        IOptions<SmsSettings> settings,
        IHttpClientFactory httpClientFactory,
        ILogger<InfobipSmsService> logger)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendAppointmentConfirmationAsync(AppointmentSmsDto dto, CancellationToken cancellationToken = default)
    {
        var (dateFormatted, timeFormatted, price) = FormatDetails(dto);

        var message = SmsTemplates.AppointmentConfirmation(
            dto.ClientName, dto.SalonName, dateFormatted, timeFormatted,
            dto.ServiceNames, dto.StaffName, dto.DurationMinutes, price,
            dto.SalonPhone);

        await SendSmsAsync(dto.ClientPhone, message, cancellationToken);
    }

    public async Task SendAppointmentReminderAsync(AppointmentSmsDto dto, CancellationToken cancellationToken = default)
    {
        var (dateFormatted, timeFormatted, _) = FormatDetails(dto);

        var message = SmsTemplates.AppointmentReminder(
            dto.ClientName, dto.SalonName, dateFormatted, timeFormatted,
            dto.ServiceNames, dto.StaffName);

        await SendSmsAsync(dto.ClientPhone, message, cancellationToken);
    }

    public async Task SendAppointmentCancellationAsync(AppointmentSmsDto dto, string? reason, CancellationToken cancellationToken = default)
    {
        var (dateFormatted, timeFormatted, _) = FormatDetails(dto);

        var message = SmsTemplates.AppointmentCancellation(
            dto.ClientName, dto.SalonName, dateFormatted, timeFormatted,
            dto.ServiceNames, reason, dto.SalonPhone);

        await SendSmsAsync(dto.ClientPhone, message, cancellationToken);
    }

    private async Task SendSmsAsync(string phoneNumber, string text, CancellationToken cancellationToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("SMS sending is disabled. Skipping SMS to {Phone}: {Message}", phoneNumber, text);
            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogWarning("Infobip API key not configured. Skipping SMS to {Phone}.", phoneNumber);
            return;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("Infobip");
            client.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/'));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("App", _settings.ApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var payload = new
            {
                messages = new[]
                {
                    new
                    {
                        destinations = new[] { new { to = phoneNumber } },
                        from = _settings.SenderName,
                        text = text
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/sms/2/text/advanced", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("SMS sent to {Phone} via Infobip.", phoneNumber);
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Infobip SMS failed with status {StatusCode}: {Response}",
                    response.StatusCode, responseBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {Phone} via Infobip.", phoneNumber);
            // Don't throw — SMS failure should not break the appointment flow
        }
    }

    private static (string Date, string Time, string Price) FormatDetails(AppointmentSmsDto dto)
    {
        var localTime = AppointmentDateTimeHelper.ToDisplayDateTime(dto.StartTime);

        var dateFormatted = localTime.ToString("dd.MM.yyyy");
        var timeFormatted = localTime.ToString("HH:mm");
        var price = $"{dto.TotalPrice:N0} {dto.Currency}";

        return (dateFormatted, timeFormatted, price);
    }
}
