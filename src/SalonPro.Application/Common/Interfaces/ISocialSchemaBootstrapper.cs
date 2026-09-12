namespace SalonPro.Application.Common.Interfaces;

public record SocialSchemaBootstrapResult(bool Success, IReadOnlyList<string> Steps);

public interface ISocialSchemaBootstrapper
{
    Task<SocialSchemaBootstrapResult> EnsureAsync(CancellationToken cancellationToken = default);
}
