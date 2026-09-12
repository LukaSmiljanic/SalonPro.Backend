namespace SalonPro.Application.Features.Social.DTOs;

public record SocialGalleryImageDto(
    Guid Id,
    string FileName,
    string ImageUrl,
    string? CaptionHint,
    long FileSizeBytes,
    DateTime CreatedAt);

public record CreatePostFromGalleryResult(SocialPostDto Post);

public record CreateGalleryAiVariantRequest(string Prompt, string? CaptionHint);

public record CreateGalleryAiVariantResult(
    SocialPostDto Post,
    string Status,
    string Message);
