namespace SalonPro.Application.Common.Interfaces;

public enum InstagramOAuthJobStatus
{
    Pending,
    Connected,
    Error,
}

public record InstagramOAuthJobResult(InstagramOAuthJobStatus Status, string? Message = null);

public interface IInstagramOAuthQueue
{
    ValueTask EnqueueAsync(Guid tenantId, string code, CancellationToken cancellationToken = default);
    InstagramOAuthJobResult? GetResult(Guid tenantId);
}
