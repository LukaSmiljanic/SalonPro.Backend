using MediatR;
using Microsoft.Extensions.Logging;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Subscriptions.Commands.ExtendSubscription;

public class ExtendSubscriptionCommandHandler : IRequestHandler<ExtendSubscriptionCommand, ExtendSubscriptionResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<ExtendSubscriptionCommandHandler> _logger;

    public ExtendSubscriptionCommandHandler(
        IUnitOfWork unitOfWork,
        IDateTimeService dateTimeService,
        ILogger<ExtendSubscriptionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    public async Task<ExtendSubscriptionResult> Handle(ExtendSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(request.TenantId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), request.TenantId);

        if (request.Days <= 0)
            return new ExtendSubscriptionResult(false, "Broj dana mora biti pozitivan.", null);

        var now = _dateTimeService.UtcNow;

        // If subscription already expired, start from now; otherwise extend from current end date
        var startFrom = tenant.SubscriptionEndDate.HasValue && tenant.SubscriptionEndDate.Value > now
            ? tenant.SubscriptionEndDate.Value
            : now;

        tenant.SubscriptionEndDate = startFrom.AddDays(request.Days);
        tenant.IsTrialing = false; // Manual extension = paid subscription

        if (!tenant.SubscriptionStartDate.HasValue)
            tenant.SubscriptionStartDate = now;

        _unitOfWork.Tenants.Update(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Subscription extended for tenant {TenantId} ({TenantName}) by {Days} days. New end: {EndDate}.",
            tenant.Id, tenant.Name, request.Days, tenant.SubscriptionEndDate);

        return new ExtendSubscriptionResult(
            true,
            $"Pretplata produžena za {request.Days} dana. Novi datum isteka: {tenant.SubscriptionEndDate:dd.MM.yyyy}",
            tenant.SubscriptionEndDate
        );
    }
}
