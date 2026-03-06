using MediatR;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Payments.Commands.CreatePayment;

public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreatePaymentCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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
        }

        await _unitOfWork.Payments.AddAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return payment.Id;
    }
}
