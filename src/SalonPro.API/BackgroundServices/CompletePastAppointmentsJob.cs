using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Appointments.Commands.CompletePastAppointments;
using SalonPro.Domain.Interfaces;
using SalonPro.Infrastructure.Persistence;

namespace SalonPro.API.BackgroundServices;

/// <summary>
/// Periodically marks past appointments as Completed for all tenants.
/// Interval is configurable via Appointments:AutoCompleteIntervalMinutes (15, 30, or 60).
/// </summary>
public class CompletePastAppointmentsJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CompletePastAppointmentsJob> _logger;

    public CompletePastAppointmentsJob(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<CompletePastAppointmentsJob> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = GetIntervalMinutes();
        _logger.LogInformation(
            "CompletePastAppointmentsJob started. Interval: {Interval} minutes.",
            intervalMinutes);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunForAllTenantsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CompletePastAppointmentsJob failed.");
            }
        }
    }

    private int GetIntervalMinutes()
    {
        var value = _configuration["Appointments:AutoCompleteIntervalMinutes"];
        if (string.IsNullOrEmpty(value))
            return 15;

        if (!int.TryParse(value, out var minutes) || minutes <= 0)
            return 15;

        return minutes switch
        {
            30 => 30,
            60 => 60,
            _ => 15
        };
    }

    private async Task RunForAllTenantsAsync(CancellationToken cancellationToken)
    {
        List<Guid> tenantIds;

        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            tenantIds = await db.Tenants.AsNoTracking().Select(t => t.Id).ToListAsync(cancellationToken);
        }

        if (tenantIds.Count == 0)
            return;

        foreach (var tenantId in tenantIds)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var tenantService = scope.ServiceProvider.GetRequiredService<ICurrentTenantService>();
                tenantService.SetTenant(tenantId);

                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var count = await mediator.Send(new CompletePastAppointmentsCommand(), cancellationToken);

                if (count > 0)
                    _logger.LogInformation(
                        "CompletePastAppointmentsJob: Tenant {TenantId} — marked {Count} appointment(s) as Completed.",
                        tenantId, count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "CompletePastAppointmentsJob: Failed for tenant {TenantId}.",
                    tenantId);
            }
        }
    }
}
