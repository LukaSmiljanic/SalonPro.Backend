using MediatR;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Social.Commands.PublishSocialPostNow;

public class PublishSocialPostNowCommandHandler : IRequestHandler<PublishSocialPostNowCommand, SocialPostPublishResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ISocialPublishService _publishService;

    public PublishSocialPostNowCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService,
        ISocialPublishService publishService)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
        _publishService = publishService;
    }

    public async Task<SocialPostPublishResult> Handle(
        PublishSocialPostNowCommand request,
        CancellationToken cancellationToken)
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
            return new SocialPostPublishResult(true, nameof(SocialPostStatus.Published), null);

        if (post.Status != SocialPostStatus.Scheduled)
            throw new Common.Exceptions.ValidationException("Samo zakazane objave mogu biti objavljene.");

        await _publishService.PublishDuePostAsync(post, cancellationToken);
        _unitOfWork.SocialPosts.Update(post);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SocialPostPublishResult(
            post.Status == SocialPostStatus.Published,
            post.Status.ToString(),
            post.FailureReason);
    }
}
