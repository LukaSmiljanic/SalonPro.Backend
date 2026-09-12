using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Infrastructure.Meta;

/// <summary>
/// Finishes Meta OAuth on a background thread so the callback can redirect immediately
/// (avoids IIS request timeout on Monster shared hosting).
/// </summary>
public class InstagramOAuthCompletionQueue : IInstagramOAuthQueue
{
    private readonly ConcurrentDictionary<Guid, InstagramOAuthJobResult> _results = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InstagramOAuthCompletionQueue> _logger;

    public InstagramOAuthCompletionQueue(
        IServiceScopeFactory scopeFactory,
        ILogger<InstagramOAuthCompletionQueue> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public ValueTask EnqueueAsync(Guid tenantId, string code, CancellationToken cancellationToken = default)
    {
        _results[tenantId] = new InstagramOAuthJobResult(InstagramOAuthJobStatus.Pending);

        _ = Task.Factory.StartNew(
            () => RunSafeAsync(tenantId, code),
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        _logger.LogInformation("Queued Instagram OAuth completion for tenant {TenantId}", tenantId);
        return ValueTask.CompletedTask;
    }

    public InstagramOAuthJobResult? GetResult(Guid tenantId) =>
        _results.TryGetValue(tenantId, out var result) ? result : null;

    private async Task RunSafeAsync(Guid tenantId, string code)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var tenantService = scope.ServiceProvider.GetRequiredService<ICurrentTenantService>();
            tenantService.SetTenant(tenantId);

            var instagram = scope.ServiceProvider.GetRequiredService<IInstagramService>();
            await instagram.CompleteOAuthAsync(tenantId, code, CancellationToken.None);

            _results[tenantId] = new InstagramOAuthJobResult(InstagramOAuthJobStatus.Connected);
            _logger.LogInformation("Instagram OAuth completed for tenant {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Instagram OAuth failed for tenant {TenantId}", tenantId);
            _results[tenantId] = new InstagramOAuthJobResult(
                InstagramOAuthJobStatus.Error,
                ex.Message);
        }
    }
}
