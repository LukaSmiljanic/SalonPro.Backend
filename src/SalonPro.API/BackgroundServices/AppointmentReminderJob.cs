using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Enums;
using SalonPro.Infrastructure.Persistence;

namespace SalonPro.API.BackgroundServices;

/// <summary>
/// Sends reminder emails 24 hours before scheduled appointments.
/// Runs every hour and picks up appointments starting in the next 23–25 hour window.
/// </summary>
public class AppointmentReminderJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AppointmentReminderJob> _logger;

    public AppointmentReminderJob(
        IServiceScopeFactory scopeFactory,
        ILogger<AppointmentReminderJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AppointmentReminderJob started. Checking every 60 minutes.");

        // Wait 2 minutes after startup to let everything initialize
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        // Run immediately on first tick, then hourly
        await SendRemindersAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await SendRemindersAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AppointmentReminderJob failed.");
            }
        }
    }

    private async Task SendRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var now = DateTime.UtcNow;
        var windowStart = now.AddHours(23);
        var windowEnd = now.AddHours(25);

        // Find all scheduled appointments in the 24h window that haven't been reminded yet
        var appointments = await context.Appointments
            .AsNoTracking()
            .Include(a => a.Client)
            .Include(a => a.StaffMember)
            .Include(a => a.Tenant)
            .Include(a => a.AppointmentServices)
                .ThenInclude(s => s.Service)
            .Where(a =>
                a.StartTime >= windowStart &&
                a.StartTime <= windowEnd &&
                (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed) &&
                !string.IsNullOrEmpty(a.Client.Email) &&
                !a.ReminderSentAt.HasValue)
            .ToListAsync(cancellationToken);

        if (appointments.Count == 0)
            return;

        _logger.LogInformation("AppointmentReminderJob: Found {Count} appointments to remind.", appointments.Count);

        foreach (var appointment in appointments)
        {
            try
            {
                var serviceNames = string.Join(", ", appointment.AppointmentServices.Select(s => s.Service.Name));

                var emailDto = new AppointmentEmailDto(
                    ClientName: appointment.Client.FullName,
                    ClientEmail: appointment.Client.Email!,
                    SalonName: appointment.Tenant.Name,
                    StaffName: appointment.StaffMember.FullName,
                    StartTime: appointment.StartTime,
                    DurationMinutes: appointment.TotalDurationMinutes,
                    ServiceNames: serviceNames,
                    TotalPrice: appointment.TotalPrice,
                    Currency: appointment.Tenant.Currency ?? "RSD",
                    SalonPhone: appointment.Tenant.Phone,
                    SalonAddress: appointment.Tenant.Address
                );

                await emailService.SendAppointmentReminderAsync(emailDto, cancellationToken);

                // Mark as reminded so we don't send again
                var tracked = await context.Appointments.FindAsync(new object[] { appointment.Id }, cancellationToken);
                if (tracked != null)
                {
                    tracked.ReminderSentAt = DateTime.UtcNow;
                    await context.SaveChangesAsync(cancellationToken);
                }

                _logger.LogInformation(
                    "Reminder sent for appointment {AppointmentId} to {Email}.",
                    appointment.Id, appointment.Client.Email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to send reminder for appointment {AppointmentId}.",
                    appointment.Id);
            }
        }
    }
}
