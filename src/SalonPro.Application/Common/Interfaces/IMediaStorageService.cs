namespace SalonPro.Application.Common.Interfaces;

public interface IMediaStorageService
{
    Task<string> SaveImageAsync(Guid tenantId, Guid fileId, byte[] imageBytes, CancellationToken cancellationToken = default);
    Task<string> SaveGalleryImageAsync(Guid tenantId, Guid imageId, byte[] imageBytes, string extension, CancellationToken cancellationToken = default);
    Task<string> SaveTenantLogoAsync(Guid tenantId, byte[] imageBytes, string extension, CancellationToken cancellationToken = default);
    Task DeleteImageAsync(Guid tenantId, Guid fileId, CancellationToken cancellationToken = default);
    Task DeleteByRelativePathAsync(string relativePath, CancellationToken cancellationToken = default);
    Task<byte[]?> ReadByRelativePathAsync(string relativePath, CancellationToken cancellationToken = default);
    string? GetPublicUrl(string? relativePath);
    /// <summary>/media/social/{tenant}/{id}.png — works with Netlify proxy.</summary>
    string ToRelativeMediaPath(string relativePath);
    /// <summary>Absolute public URL — uses stored value, or discovers file on disk when DB column is empty.</summary>
    string? ResolveImageUrl(Guid tenantId, Guid postId, string? storedUrl);
}
