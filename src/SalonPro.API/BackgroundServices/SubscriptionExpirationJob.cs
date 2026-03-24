using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Infrastructure.Persistence;

namespace SalonPro.API.BackgroundServices;

/// <summary>
/// Background job that runs daily to:
/// 1. Deactivate tenants whose subscription has expired (sets IsActive = false)
/// 2. Send warning emails 3 days before expiry
/// 3. Send expiration notification email when subscription expires
/// </summary>
public class SubscriptionExpirationJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionExpirationJob> _logger;

    public SubscriptionExpirationJob(
        IServiceScopeFactory scopeFactory,
        ILogger<SubscriptionExpirationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SubscriptionExpirationJob started. Checking every 6 hours.");

        // Wait 5 minutes after startup
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        // Run immediately on first tick, then every 6 hours
        await CheckSubscriptionsAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(6));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CheckSubscriptionsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SubscriptionExpirationJob failed.");
            }
        }
    }

    private async Task CheckSubscriptionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var now = DateTime.UtcNow;

        // 1. Deactivate expired subscriptions
        var expiredTenants = await context.Tenants
            .Where(t => t.IsActive &&
                        t.SubscriptionEndDate.HasValue &&
                        t.SubscriptionEndDate.Value <= now)
            .ToListAsync(cancellationToken);

        foreach (var tenant in expiredTenants)
        {
            tenant.IsActive = false;
            _logger.LogInformation(
                "Deactivated tenant {TenantId} ({TenantName}) — subscription expired on {EndDate}.",
                tenant.Id, tenant.Name, tenant.SubscriptionEndDate);

            // Send expiration notification email
            if (!string.IsNullOrWhiteSpace(tenant.Email))
            {
                try
                {
                    await emailService.SendSubscriptionExpiredAsync(
                        tenant.Email, tenant.Name, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to send expiration email to {Email} for tenant {TenantName}.",
                        tenant.Email, tenant.Name);
                }
            }
        }

        if (expiredTenants.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deactivated {Count} tenant(s) with expired subscriptions.", expiredTenants.Count);
        }

        // 2. Send warning emails for subscriptions expiring in 3 days
        var warningDate = now.AddDays(3);
        var warningTenants = await context.Tenants
            .Where(t => t.IsActive &&
                        t.SubscriptionEndDate.HasValue &&
                        t.SubscriptionEndDate.Value > now &&
                        t.SubscriptionEndDate.Value <= warningDate &&
                        !string.IsNullOrEmpty(t.Email))
            .ToListAsync(cancellationToken);

        foreach (var tenant in warningTenants)
        {
            try
            {
                var daysLeft = (int)Math.Ceiling((tenant.SubscriptionEndDate!.Value - now).TotalDays);
                await emailService.SendSubscriptionWarningAsync(
                    tenant.Email!, tenant.Name, daysLeft, tenant.SubscriptionEndDate.Value, cancellationToken);

                _logger.LogInformation(
                    "Sent subscription warning to {Email} for tenant {TenantName} — {Days} day(s) remaining.",
                    tenant.Email, tenant.Name, daysLeft);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to send warning email to {Email} for tenant {TenantName}.",
                    tenant.Email, tenant.Name);
            }
        }

        // 3. Reactivate tenants whose subscription was renewed (extended) while inactive
        var reactivatedTenants = await context.Tenants
            .Where(t => !t.IsActive &&
                        t.SubscriptionEndDate.HasValue &&
                        t.SubscriptionEndDate.Value > now)
            .ToListAsync(cancellationToken);

        foreach (var tenant in reactivatedTenants)
        {
            tenant.IsActive = true;
            _logger.LogInformation(
                "Reactivated tenant {TenantId} ({TenantName}) — subscription renewed until {EndDate}.",
                tenant.Id, tenant.Name, tenant.SubscriptionEndDate);
        }

        if (reactivatedTenants.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Reactivated {Count} tenant(s) with renewed subscriptions.", reactivatedTenants.Count);
        }
    }
}
