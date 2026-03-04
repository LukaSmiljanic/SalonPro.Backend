using MediatR;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Services.Commands.UpdateService;

public class UpdateServiceCommandHandler : IRequestHandler<UpdateServiceCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateServiceCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateServiceCommand request, CancellationToken cancellationToken)
    {
        var service = await _unitOfWork.Services.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Service), request.Id);

        var category = await _unitOfWork.ServiceCategories.GetByIdAsync(request.ServiceCategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceCategory), request.ServiceCategoryId);

        service.Name = request.Name;
        service.Description = request.Description;
        service.Price = request.Price;
        service.DurationMinutes = request.DurationMinutes;
        service.ServiceCategoryId = request.ServiceCategoryId;
        service.IsActive = request.IsActive;
        service.LastModifiedAt = DateTime.UtcNow;

        _unitOfWork.Services.Update(service);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
