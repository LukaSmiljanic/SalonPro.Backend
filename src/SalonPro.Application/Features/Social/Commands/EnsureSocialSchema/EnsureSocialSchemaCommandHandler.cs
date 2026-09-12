using MediatR;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Application.Features.Social;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Social.Commands.EnsureSocialSchema;

public record EnsureSocialSchemaCommand : IRequest<SocialSchemaBootstrapResult>;

public class EnsureSocialSchemaCommandHandler : IRequestHandler<EnsureSocialSchemaCommand, SocialSchemaBootstrapResult>
{
    private readonly ISocialSchemaBootstrapper _bootstrapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;

    public EnsureSocialSchemaCommandHandler(
        ISocialSchemaBootstrapper bootstrapper,
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService)
    {
        _bootstrapper = bootstrapper;
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
    }

    public async Task<SocialSchemaBootstrapResult> Handle(
        EnsureSocialSchemaCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        SocialPlanGuard.EnsureAllowed(tenant);

        return await _bootstrapper.EnsureAsync(cancellationToken);
    }
}
