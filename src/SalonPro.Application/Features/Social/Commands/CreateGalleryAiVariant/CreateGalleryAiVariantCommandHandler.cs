using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Application.Features.Social.DTOs;
using SalonPro.Application.Features.Social.Queries.GetSocialPosts;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Social.Commands.CreateGalleryAiVariant;

public record CreateGalleryAiVariantCommand(Guid ImageId, string Prompt, string? CaptionHint)
    : IRequest<CreateGalleryAiVariantResult>;

public class CreateGalleryAiVariantCommandHandler
    : IRequestHandler<CreateGalleryAiVariantCommand, CreateGalleryAiVariantResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly IOpenAiService _openAiService;
    private readonly IMediaStorageService _mediaStorage;
    private readonly ISocialImageQueue _imageQueue;

    public CreateGalleryAiVariantCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService,
        IOpenAiService openAiService,
        IMediaStorageService mediaStorage,
        ISocialImageQueue imageQueue)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
        _openAiService = openAiService;
        _mediaStorage = mediaStorage;
        _imageQueue = imageQueue;
    }

    public async Task<CreateGalleryAiVariantResult> Handle(
        CreateGalleryAiVariantCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        SocialPlanGuard.EnsureAllowed(tenant);

        if (!_openAiService.ImageGenerationEnabled)
            throw new ValidationException("OpenAI generisanje slika nije konfigurisano.");

        var prompt = request.Prompt?.Trim();
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ValidationException("Unesite prompt za AI varijantu slike.");

        var image = await _unitOfWork.SocialGalleryImages.Query()
            .FirstOrDefaultAsync(g => g.Id == request.ImageId && g.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Slika nije pronađena.");

        var hint = string.IsNullOrWhiteSpace(request.CaptionHint) ? image.CaptionHint : request.CaptionHint.Trim();
        var tz = SalonTimeZoneHelper.Resolve(tenant.TimeZone);
        var tomorrow = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
        var scheduledUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(tomorrow, DateTimeKind.Unspecified), tz);

        var post = new SocialPost
        {
            TenantId = tenantId,
            Topic = "AI varijanta",
            Caption = "⏳ AI generiše sliku i tekst objave…",
            Hashtags = string.Empty,
            ImagePrompt = prompt,
            ScheduledAt = scheduledUtc,
            Status = SocialPostStatus.Draft,
        };

        await _unitOfWork.SocialPosts.AddAsync(post, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _imageQueue.EnqueueAsync(new SocialImageJob(
            tenantId,
            post.Id,
            prompt,
            image.Id,
            hint), cancellationToken);

        return new CreateGalleryAiVariantResult(
            SocialPostMapper.ToDto(post, _mediaStorage),
            "queued",
            "AI varijanta se generiše u pozadini (obično 30–120 sek). Pogledajte listu objava ispod.");
    }
}
