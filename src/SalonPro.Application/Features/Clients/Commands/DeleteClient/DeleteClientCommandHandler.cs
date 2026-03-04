using MediatR;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Clients.Commands.DeleteClient;

public class DeleteClientCommandHandler : IRequestHandler<DeleteClientCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteClientCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteClientCommand request, CancellationToken cancellationToken)
    {
        var client = await _unitOfWork.Clients.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Client), request.Id);

        _unitOfWork.Clients.Delete(client);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
