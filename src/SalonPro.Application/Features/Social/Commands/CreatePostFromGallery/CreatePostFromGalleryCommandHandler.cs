using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Application.Features.Social.DTOs;
using SalonPro.Application.Features.Social.Queries.GetSocialPosts;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Social.Commands.CreatePostFromGallery;

public record CreatePostFromGalleryCommand(Guid ImageId, string? CaptionHint) : IRequest<CreatePostFromGalleryResult>;

public class CreatePostFromGalleryCommandHandler
    : IRequestHandler<CreatePostFromGalleryCommand, CreatePostFromGalleryResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ISocialContentGenerator _contentGenerator;
    private readonly IMediaStorageService _mediaStorage;

    public CreatePostFromGalleryCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService,
        ISocialContentGenerator contentGenerator,
        IMediaStorageService mediaStorage)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
        _contentGenerator = contentGenerator;
        _mediaStorage = mediaStorage;
    }

    public async Task<CreatePostFromGalleryResult> Handle(
        CreatePostFromGalleryCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        SocialPlanGuard.EnsureAllowed(tenant);

        var image = await _unitOfWork.SocialGalleryImages.Query()
            .FirstOrDefaultAsync(g => g.Id == request.ImageId && g.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Slika nije pronađena.");

        var hint = string.IsNullOrWhiteSpace(request.CaptionHint) ? image.CaptionHint : request.CaptionHint.Trim();

        var imageBytes = await _mediaStorage.ReadByRelativePathAsync(image.RelativePath, cancellationToken)
            ?? throw new ValidationException("Slika nije pronađena na disku.");

        var draft = await _contentGenerator.GenerateFromGalleryAsync(
            tenantId,
            hint,
            imageBytes,
            image.ContentType,
            cancellationToken);

        var post = new SocialPost
        {
            TenantId = tenantId,
            Topic = draft.Topic,
            Caption = draft.Caption,
            Hashtags = draft.Hashtags,
            ImagePrompt = null,
            ImageUrl = image.RelativePath,
            ScheduledAt = draft.ScheduledAtUtc,
            Status = SocialPostStatus.Draft,
        };

        await _unitOfWork.SocialPosts.AddAsync(post, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreatePostFromGalleryResult(SocialPostMapper.ToDto(post, _mediaStorage));
    }
}
