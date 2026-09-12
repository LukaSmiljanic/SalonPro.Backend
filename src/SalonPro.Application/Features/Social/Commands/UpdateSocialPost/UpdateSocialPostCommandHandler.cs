using MediatR;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Application.Features.Social.DTOs;
using SalonPro.Application.Features.Social.Queries.GetSocialPosts;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Social.Commands.UpdateSocialPost;

public record UpdateSocialPostCommand(
    Guid Id,
    string Caption,
    string Hashtags,
    DateTime ScheduledAt) : IRequest<SocialPostDto>;

public class UpdateSocialPostCommandHandler : IRequestHandler<UpdateSocialPostCommand, SocialPostDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly IMediaStorageService _mediaStorage;

    public UpdateSocialPostCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService,
        IMediaStorageService mediaStorage)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
        _mediaStorage = mediaStorage;
    }

    public async Task<SocialPostDto> Handle(UpdateSocialPostCommand request, CancellationToken cancellationToken)
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
            throw new Common.Exceptions.ValidationException("Objavljene postove nije moguće menjati.");

        post.Caption = request.Caption.Trim();
        post.Hashtags = request.Hashtags.Trim();
        post.ScheduledAt = request.ScheduledAt;

        _unitOfWork.SocialPosts.Update(post);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SocialPostMapper.ToDto(post, _mediaStorage);
    }
}
