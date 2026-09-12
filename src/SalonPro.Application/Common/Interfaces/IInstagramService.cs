namespace SalonPro.Application.Common.Interfaces;

public record InstagramConnectionStatus(
    bool IsConnected,
    string? Username,
    DateTime? TokenExpiresAt);

public record InstagramConnectUrl(string Url);

public interface IInstagramService
{
    bool IsConfigured { get; }
    string? AppId { get; }
    string? RedirectUri { get; }
    IReadOnlyList<string> GetRequestedScopes();
    string BuildConnectUrl(string state);
    Task<InstagramConnectionStatus> GetConnectionStatusAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task CompleteOAuthAsync(Guid tenantId, string code, CancellationToken cancellationToken = default);
    Task DisconnectAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<string> PublishImagePostAsync(Guid tenantId, string imageUrl, string caption, CancellationToken cancellationToken = default);
}
