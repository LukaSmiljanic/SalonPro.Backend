namespace SalonPro.Application.Common.Interfaces;

public record SocialImageJob(
    Guid TenantId,
    Guid PostId,
    string Prompt,
    Guid? SourceGalleryImageId = null,
    string? CaptionHint = null);

public interface ISocialImageQueue
{
    ValueTask EnqueueAsync(SocialImageJob job, CancellationToken cancellationToken = default);
}
