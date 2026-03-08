using MediatR;

namespace SalonPro.Application.Features.Subscriptions.Commands.ExtendSubscription;

public record ExtendSubscriptionCommand(Guid TenantId, int Days) : IRequest<ExtendSubscriptionResult>;

public record ExtendSubscriptionResult(
    bool Success,
    string Message,
    DateTime? NewEndDate
);
