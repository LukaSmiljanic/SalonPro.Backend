namespace SalonPro.Application.Common.Interfaces;

public record SocialPostDraft(
    string Topic,
    string Caption,
    string Hashtags,
    string ImagePrompt,
    DateTime ScheduledAtUtc);

public interface ISocialContentGenerator
{
    Task<IReadOnlyList<SocialPostDraft>> GenerateWeekAsync(
        Guid tenantId,
        DateTime weekStartLocalDate,
        CancellationToken cancellationToken = default);

    Task<SocialPostDraft> GenerateFromGalleryAsync(
        Guid tenantId,
        string? captionHint,
        byte[]? imageBytes = null,
        string? imageContentType = null,
        CancellationToken cancellationToken = default);
}
