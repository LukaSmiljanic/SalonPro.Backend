namespace SalonPro.Application.Common.Interfaces;

public record SocialDbDiagnosticsDto(
    string DatabaseName,
    string ServerName,
    bool SocialPostsTableExists,
    bool TenantInstagramAccountsTableExists,
    IReadOnlyList<string> SocialPostsColumns,
    IReadOnlyList<string> TenantInstagramColumns,
    int? SocialPostsRowCount,
    string? SocialPostsQueryError,
    string? TenantInstagramQueryError);

public interface ISocialDatabaseDiagnostics
{
    Task<SocialDbDiagnosticsDto> InspectAsync(CancellationToken cancellationToken = default);
}
