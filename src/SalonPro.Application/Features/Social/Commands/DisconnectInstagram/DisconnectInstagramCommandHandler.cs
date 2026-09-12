using MediatR;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Social.Commands.DisconnectInstagram;

public record DisconnectInstagramCommand : IRequest<Unit>;

public class DisconnectInstagramCommandHandler : IRequestHandler<DisconnectInstagramCommand, Unit>
{
    private readonly ICurrentTenantService _currentTenantService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInstagramService _instagramService;

    public DisconnectInstagramCommandHandler(
        ICurrentTenantService currentTenantService,
        IUnitOfWork unitOfWork,
        IInstagramService instagramService)
    {
        _currentTenantService = currentTenantService;
        _unitOfWork = unitOfWork;
        _instagramService = instagramService;
    }

    public async Task<Unit> Handle(DisconnectInstagramCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        SocialPlanGuard.EnsureAllowed(tenant);

        await _instagramService.DisconnectAsync(tenantId, cancellationToken);
        return Unit.Value;
    }
}
