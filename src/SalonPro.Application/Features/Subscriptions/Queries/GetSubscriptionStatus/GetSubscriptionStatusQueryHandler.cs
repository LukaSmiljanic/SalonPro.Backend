using MediatR;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Application.Features.Subscriptions.Commands.ExtendSubscription;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Subscriptions.Queries.GetSubscriptionStatus;

public class GetSubscriptionStatusQueryHandler : IRequestHandler<GetSubscriptionStatusQuery, SubscriptionStatusDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeService _dateTimeService;

    public GetSubscriptionStatusQueryHandler(IUnitOfWork unitOfWork, IDateTimeService dateTimeService)
    {
        _unitOfWork = unitOfWork;
        _dateTimeService = dateTimeService;
    }

    public async Task<SubscriptionStatusDto> Handle(GetSubscriptionStatusQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(request.TenantId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), request.TenantId);

        var now = _dateTimeService.UtcNow;
        string status;
        int? daysRemaining = null;

        if (!tenant.EmailVerified)
        {
            status = "PendingVerification";
        }
        else if (tenant.IsTrialing && tenant.HasActiveSubscription)
        {
            status = "Trial";
            daysRemaining = (int)(tenant.SubscriptionEndDate!.Value - now).TotalDays;
        }
        else if (tenant.HasActiveSubscription)
        {
            status = "Active";
            daysRemaining = (int)(tenant.SubscriptionEndDate!.Value - now).TotalDays;
        }
        else
        {
            status = "Expired";
            daysRemaining = 0;
        }

        return new SubscriptionStatusDto(
            TenantId: tenant.Id,
            TenantName: tenant.Name,
            EmailVerified: tenant.EmailVerified,
            IsTrialing: tenant.IsTrialing,
            Status: status,
            SubscriptionStartDate: tenant.SubscriptionStartDate,
            SubscriptionEndDate: tenant.SubscriptionEndDate,
            DaysRemaining: daysRemaining
        );
    }
}
