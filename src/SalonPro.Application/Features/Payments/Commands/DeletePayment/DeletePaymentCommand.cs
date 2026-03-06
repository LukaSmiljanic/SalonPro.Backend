using MediatR;

namespace SalonPro.Application.Features.Payments.Commands.DeletePayment;

public record DeletePaymentCommand(Guid Id) : IRequest<Unit>;
