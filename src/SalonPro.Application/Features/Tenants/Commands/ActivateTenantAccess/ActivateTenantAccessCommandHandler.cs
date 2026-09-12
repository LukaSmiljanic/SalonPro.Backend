using MediatR;
using SalonPro.Application.Common;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Tenants.Commands.ActivateTenantAccess;

public class ActivateTenantAccessCommandHandler : IRequestHandler<ActivateTenantAccessCommand, ActivateTenantAccessResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeService _dateTimeService;

    public ActivateTenantAccessCommandHandler(IUnitOfWork unitOfWork, IDateTimeService dateTimeService)
    {
        _unitOfWork = unitOfWork;
        _dateTimeService = dateTimeService;
    }

    public async Task<ActivateTenantAccessResult> Handle(ActivateTenantAccessCommand request, CancellationToken cancellationToken)
    {
        if (request.TrialDays <= 0)
            return new ActivateTenantAccessResult(false, "Broj dana mora biti pozitivan.", null);

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(request.TenantId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), request.TenantId);

        var now = _dateTimeService.UtcNow;

        tenant.EmailVerified = true;
        tenant.EmailVerificationToken = null;
        tenant.EmailVerificationTokenExpiry = null;
        tenant.IsActive = true;
        tenant.IsTrialing = true;
        tenant.Plan = TenantPlanRules.Demo;
        tenant.SubscriptionExpiryWarningSentUtc = null;

        if (!tenant.SubscriptionStartDate.HasValue)
            tenant.SubscriptionStartDate = now;

        tenant.SubscriptionEndDate = now.AddDays(request.TrialDays);

        _unitOfWork.Tenants.Update(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ActivateTenantAccessResult(
            true,
            $"Nalog aktiviran. Trial do {tenant.SubscriptionEndDate:dd.MM.yyyy}.",
            tenant.SubscriptionEndDate);
    }
}
