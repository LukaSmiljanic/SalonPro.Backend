using MediatR;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Social.Commands.ScheduleSocialPost;

public record ScheduleSocialPostCommand(Guid Id) : IRequest<Unit>;

public class ScheduleSocialPostCommandHandler : IRequestHandler<ScheduleSocialPostCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;

    public ScheduleSocialPostCommandHandler(IUnitOfWork unitOfWork, ICurrentTenantService currentTenantService)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
    }

    public async Task<Unit> Handle(ScheduleSocialPostCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        SocialPlanGuard.EnsureAllowed(tenant);

        var post = await _unitOfWork.SocialPosts.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new Common.Exceptions.NotFoundException("Objava nije pronađena.");

        if (post.TenantId != tenantId)
            throw new Common.Exceptions.NotFoundException("Objava nije pronađena.");

        if (post.Status == SocialPostStatus.Published)
            throw new Common.Exceptions.ValidationException("Objava je već objavljena.");

        if (string.IsNullOrWhiteSpace(post.Caption))
            throw new Common.Exceptions.ValidationException("Tekst objave ne može biti prazan.");

        post.Status = SocialPostStatus.Scheduled;
        _unitOfWork.SocialPosts.Update(post);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
