using MediatR;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Services.Commands.CreateService;

public class CreateServiceCommandHandler : IRequestHandler<CreateServiceCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;

    public CreateServiceCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
    }

    public async Task<Guid> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Tenant context is not set.");

        var category = await _unitOfWork.ServiceCategories.GetByIdAsync(request.ServiceCategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceCategory), request.ServiceCategoryId);

        var service = new Service
        {
            TenantId = tenantId,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            DurationMinutes = request.DurationMinutes,
            ServiceCategoryId = request.ServiceCategoryId,
            IsActive = true
        };

        await _unitOfWork.Services.AddAsync(service, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return service.Id;
    }
}
