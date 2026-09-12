using SalonPro.Domain.Enums;

namespace SalonPro.Application.Features.Social.DTOs;

public record SocialPostDto(
    Guid Id,
    DateTime ScheduledAt,
    string Topic,
    string Caption,
    string Hashtags,
    string? ImagePrompt,
    string? ImageUrl,
    SocialPostStatus Status,
    DateTime? PublishedAt,
    string? FailureReason,
    string? InstagramMediaId);

public record SocialPostsWeekDto(
    IReadOnlyList<SocialPostDto> Posts,
    bool InstagramConnected,
    string? InstagramUsername,
    bool OpenAiConfigured);
