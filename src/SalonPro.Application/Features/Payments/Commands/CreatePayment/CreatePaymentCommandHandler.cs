using MediatR;
using Microsoft.Extensions.Logging;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Payments.Commands.CreatePayment;

public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreatePaymentCommandHandler> _logger;

    public CreatePaymentCommandHandler(IUnitOfWork unitOfWork, ILogger<CreatePaymentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = new Payment
        {
            TenantId = request.TenantId,
            Amount = request.Amount,
            Currency = request.Currency,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            Status = request.Status,
            Notes = request.Notes,
            PaidBy = request.PaidBy
        };

        if (request.Status == PaymentStatus.Paid)
        {
            payment.PaidAt = DateTime.UtcNow;

            // Automatically extend tenant subscription
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(request.TenantId, cancellationToken);
            if (tenant != null)
            {
                var now = DateTime.UtcNow;

                if (tenant.SubscriptionEndDate.HasValue && tenant.SubscriptionEndDate.Value > now)
                {
                    if (request.PeriodEnd > tenant.SubscriptionEndDate.Value)
                        tenant.SubscriptionEndDate = request.PeriodEnd;
                }
                else
                {
                    tenant.SubscriptionEndDate = request.PeriodEnd;
                }

                if (!tenant.SubscriptionStartDate.HasValue)
                    tenant.SubscriptionStartDate = request.PeriodStart;

                tenant.IsTrialing = false;
                tenant.IsActive = true;
                tenant.SubscriptionExpiryWarningSentUtc = null;

                _unitOfWork.Tenants.Update(tenant);

                _logger.LogInformation(
                    "Payment created as Paid. Tenant {TenantName} subscription extended to {EndDate}.",
                    tenant.Name, tenant.SubscriptionEndDate);
            }
        }

        await _unitOfWork.Payments.AddAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return payment.Id;
    }
}
