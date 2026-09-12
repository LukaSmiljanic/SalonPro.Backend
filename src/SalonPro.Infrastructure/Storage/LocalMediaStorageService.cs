using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Infrastructure.Storage;

namespace SalonPro.Infrastructure.Storage;

public class LocalMediaStorageService : IMediaStorageService
{
    private readonly SocialStorageSettings _settings;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<LocalMediaStorageService> _logger;

    public LocalMediaStorageService(
        IOptions<SocialStorageSettings> settings,
        IHostEnvironment environment,
        ILogger<LocalMediaStorageService> logger)
    {
        _settings = settings.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task<string> SaveGalleryImageAsync(
        Guid tenantId,
        Guid imageId,
        byte[] imageBytes,
        string extension,
        CancellationToken cancellationToken = default)
    {
        var ext = NormalizeExtension(extension);
        var root = ResolveStorageRoot();
        var galleryDir = Path.Combine(root, tenantId.ToString("N"), "gallery");
        Directory.CreateDirectory(galleryDir);

        var fileName = $"{imageId:N}{ext}";
        var fullPath = Path.Combine(galleryDir, fileName);
        await File.WriteAllBytesAsync(fullPath, imageBytes, cancellationToken);

        var relative = $"{tenantId:N}/gallery/{fileName}";
        _logger.LogInformation("Saved gallery image {Path}", relative);
        return relative;
    }

    public async Task<string> SaveTenantLogoAsync(
        Guid tenantId,
        byte[] imageBytes,
        string extension,
        CancellationToken cancellationToken = default)
    {
        var ext = NormalizeExtension(extension, allowSvg: true);
        var root = ResolveStorageRoot();
        var tenantDir = Path.Combine(root, tenantId.ToString("N"));
        Directory.CreateDirectory(tenantDir);

        foreach (var existing in Directory.GetFiles(tenantDir, "logo.*"))
            File.Delete(existing);

        var fileName = $"logo{ext}";
        var fullPath = Path.Combine(tenantDir, fileName);
        await File.WriteAllBytesAsync(fullPath, imageBytes, cancellationToken);

        var relative = $"{tenantId:N}/{fileName}";
        _logger.LogInformation("Saved tenant logo {Path}", relative);
        return relative;
    }

    public Task DeleteByRelativePathAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return Task.CompletedTask;

        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        var fullPath = Path.Combine(ResolveStorageRoot(), normalized.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("Deleted media file {Path}", normalized);
        }

        return Task.CompletedTask;
    }

    public Task<byte[]?> ReadByRelativePathAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return Task.FromResult<byte[]?>(null);

        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        var fullPath = Path.Combine(ResolveStorageRoot(), normalized.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
            return Task.FromResult<byte[]?>(null);

        return ReadFileBytesAsync(fullPath, cancellationToken);
    }

    private static async Task<byte[]?> ReadFileBytesAsync(string fullPath, CancellationToken cancellationToken)
    {
        var bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken);
        return bytes;
    }

    private static string NormalizeExtension(string extension, bool allowSvg = false)
    {
        var ext = extension.Trim().ToLowerInvariant();
        if (!ext.StartsWith('.'))
            ext = "." + ext;

        if (allowSvg && ext == ".svg")
            return ext;

        return ext switch
        {
            ".jpg" or ".jpeg" or ".png" or ".webp" => ext,
            _ => ".jpg",
        };
    }

    public async Task<string> SaveImageAsync(
        Guid tenantId,
        Guid fileId,
        byte[] imageBytes,
        CancellationToken cancellationToken = default)
    {
        var root = ResolveStorageRoot();
        var tenantDir = Path.Combine(root, tenantId.ToString("N"));
        Directory.CreateDirectory(tenantDir);

        var fileName = $"{fileId:N}.png";
        var fullPath = Path.Combine(tenantDir, fileName);
        await File.WriteAllBytesAsync(fullPath, imageBytes, cancellationToken);

        var relative = $"{tenantId:N}/{fileName}";
        _logger.LogInformation("Saved social image {Path}", relative);
        return relative;
    }

    public Task DeleteImageAsync(Guid tenantId, Guid fileId, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(ResolveStorageRoot(), tenantId.ToString("N"), $"{fileId:N}.png");
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("Deleted social image {TenantId}/{FileId}", tenantId, fileId);
        }

        return Task.CompletedTask;
    }

    public string ToRelativeMediaPath(string relativePath) =>
        $"/media/social/{relativePath.Replace('\\', '/')}";

    public string? GetPublicUrl(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        var baseUrl = (_settings.ApiPublicUrl ?? string.Empty).TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            return $"/media/social/{relativePath.Replace('\\', '/')}";

        return $"{baseUrl}/media/social/{relativePath.Replace('\\', '/')}";
    }

    public string? ResolveImageUrl(Guid tenantId, Guid postId, string? storedUrl)
    {
        if (!string.IsNullOrWhiteSpace(storedUrl))
        {
            var trimmed = storedUrl.Trim();
            var proxied = TryExtractMediaPath(trimmed);
            if (proxied != null)
                return proxied;

            if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // Legacy absolute URL in DB — normalize to relative for frontend proxy.
                proxied = TryExtractMediaPath(trimmed);
                if (proxied != null) return proxied;
                return trimmed;
            }

            return ToRelativeMediaPath(trimmed.TrimStart('/'));
        }

        var relative = $"{tenantId:N}/{postId:N}.png";
        var fullPath = Path.Combine(ResolveStorageRoot(), tenantId.ToString("N"), $"{postId:N}.png");
        if (!File.Exists(fullPath))
            return null;

        return ToRelativeMediaPath(relative);
    }

    private static string? TryExtractMediaPath(string url)
    {
        const string marker = "/media/social/";
        var idx = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        return url[idx..].Replace('\\', '/');
    }

    private string ResolveStorageRoot()
    {
        var path = _settings.StoragePath;
        if (Path.IsPathRooted(path))
            return path;

        return Path.Combine(_environment.ContentRootPath, path);
    }

    public static string GetPhysicalRoot(IHostEnvironment env, SocialStorageSettings settings)
    {
        if (Path.IsPathRooted(settings.StoragePath))
            return settings.StoragePath;
        return Path.Combine(env.ContentRootPath, settings.StoragePath);
    }
}
