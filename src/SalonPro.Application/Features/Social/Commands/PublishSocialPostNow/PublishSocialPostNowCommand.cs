using MediatR;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Social.Commands.PublishSocialPostNow;

public record PublishSocialPostNowCommand(Guid Id) : IRequest<SocialPostPublishResult>;

public record SocialPostPublishResult(bool Success, string Status, string? FailureReason);
