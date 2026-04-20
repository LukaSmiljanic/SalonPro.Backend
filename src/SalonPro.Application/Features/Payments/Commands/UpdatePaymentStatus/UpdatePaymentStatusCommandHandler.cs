using MediatR;
using Microsoft.Extensions.Logging;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Payments.Commands.UpdatePaymentStatus;

public class UpdatePaymentStatusCommandHandler : IRequestHandler<UpdatePaymentStatusCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdatePaymentStatusCommandHandler> _logger;

    public UpdatePaymentStatusCommandHandler(IUnitOfWork unitOfWork, ILogger<UpdatePaymentStatusCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdatePaymentStatusCommand request, CancellationToken cancellationToken)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), request.Id);

        var previousStatus = payment.Status;
        payment.Status = request.Status;
        payment.UpdatedAt = DateTime.UtcNow;

        if (request.Status == PaymentStatus.Paid)
        {
            payment.PaidAt = DateTime.UtcNow;

            // When marking as Paid, automatically extend tenant subscription to PeriodEnd
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(payment.TenantId, cancellationToken);
            if (tenant != null)
            {
                var now = DateTime.UtcNow;

                // Extend subscription to this payment's period end date
                // If current subscription is still active, use the later of current end and payment period end
                if (tenant.SubscriptionEndDate.HasValue && tenant.SubscriptionEndDate.Value > now)
                {
                    if (payment.PeriodEnd > tenant.SubscriptionEndDate.Value)
                        tenant.SubscriptionEndDate = payment.PeriodEnd;
                }
                else
                {
                    // Subscription expired or not set — set to payment period end
                    tenant.SubscriptionEndDate = payment.PeriodEnd;
                }

                if (!tenant.SubscriptionStartDate.HasValue)
                    tenant.SubscriptionStartDate = payment.PeriodStart;

                tenant.IsTrialing = false;
                tenant.IsActive = true;
                tenant.SubscriptionExpiryWarningSentUtc = null;

                _unitOfWork.Tenants.Update(tenant);

                _logger.LogInformation(
                    "Payment {PaymentId} marked as Paid. Tenant {TenantName} subscription extended to {EndDate}.",
                    payment.Id, tenant.Name, tenant.SubscriptionEndDate);
            }
        }

        if (request.Notes != null)
        {
            payment.Notes = request.Notes;
        }

        if (request.PaidBy != null)
        {
            payment.PaidBy = request.PaidBy;
        }

        _unitOfWork.Payments.Update(payment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
