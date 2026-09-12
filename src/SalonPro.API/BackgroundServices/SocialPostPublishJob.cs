using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Enums;
using SalonPro.Infrastructure.Persistence;
using SalonPro.Infrastructure.Storage;

namespace SalonPro.API.BackgroundServices;

public class SocialPostPublishJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SocialStorageSettings _settings;
    private readonly ILogger<SocialPostPublishJob> _logger;

    public SocialPostPublishJob(
        IServiceScopeFactory scopeFactory,
        IOptions<SocialStorageSettings> settings,
        ILogger<SocialPostPublishJob> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.PublishJobEnabled)
        {
            _logger.LogInformation("SocialPostPublishJob is disabled.");
            return;
        }

        _logger.LogInformation("SocialPostPublishJob started. Checking every 1 minute.");

        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        await PublishDuePostsAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await PublishDuePostsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SocialPostPublishJob failed.");
            }
        }
    }

    private async Task PublishDuePostsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var publishService = scope.ServiceProvider.GetRequiredService<ISocialPublishService>();
        var now = DateTime.UtcNow;

        var due = await context.SocialPosts
            .Where(p => p.Status == SocialPostStatus.Scheduled && p.ScheduledAt <= now)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (due.Count == 0)
            return;

        foreach (var post in due)
            await publishService.PublishDuePostAsync(post, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }
}
