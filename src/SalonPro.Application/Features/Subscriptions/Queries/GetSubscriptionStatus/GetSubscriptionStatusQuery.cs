using MediatR;

namespace SalonPro.Application.Features.Subscriptions.Commands.ExtendSubscription;

public record GetSubscriptionStatusQuery(Guid TenantId) : IRequest<SubscriptionStatusDto>;

public record SubscriptionStatusDto(
    Guid TenantId,
    string TenantName,
    bool EmailVerified,
    bool IsTrialing,
    string Status,
    DateTime? SubscriptionStartDate,
    DateTime? SubscriptionEndDate,
    int? DaysRemaining
);
