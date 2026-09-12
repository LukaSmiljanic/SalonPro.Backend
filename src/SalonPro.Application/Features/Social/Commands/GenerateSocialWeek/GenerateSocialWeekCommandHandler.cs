using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Application.Features.Social.DTOs;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Social.Commands.GenerateSocialWeek;

public record GenerateSocialWeekCommand(DateTime? WeekStartLocalDate) : IRequest<SocialPostsWeekDto>;

public class GenerateSocialWeekCommandHandler : IRequestHandler<GenerateSocialWeekCommand, SocialPostsWeekDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ISocialContentGenerator _contentGenerator;
    private readonly IDateTimeService _dateTimeService;
    private readonly IOpenAiService _openAiService;
    private readonly IInstagramService _instagramService;
    private readonly ISocialImageQueue _imageQueue;
    private readonly IMediaStorageService _mediaStorage;

    public GenerateSocialWeekCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService,
        ISocialContentGenerator contentGenerator,
        IDateTimeService dateTimeService,
        IOpenAiService openAiService,
        IInstagramService instagramService,
        ISocialImageQueue imageQueue,
        IMediaStorageService mediaStorage)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
        _contentGenerator = contentGenerator;
        _dateTimeService = dateTimeService;
        _openAiService = openAiService;
        _instagramService = instagramService;
        _imageQueue = imageQueue;
        _mediaStorage = mediaStorage;
    }

    public async Task<SocialPostsWeekDto> Handle(GenerateSocialWeekCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        SocialPlanGuard.EnsureAllowed(tenant);

        var weekStart = (request.WeekStartLocalDate ?? _dateTimeService.UtcNow.Date).Date;
        var weekEnd = weekStart.AddDays(7);

        var tz = SalonTimeZoneHelper.Resolve(tenant?.TimeZone);
        var weekStartUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(weekStart, DateTimeKind.Unspecified), tz);
        var weekEndUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(weekEnd, DateTimeKind.Unspecified), tz);

        var existing = await _unitOfWork.SocialPosts.Query()
            .Where(p => p.TenantId == tenantId
                && p.ScheduledAt >= weekStartUtc
                && p.ScheduledAt < weekEndUtc
                && p.Status != SocialPostStatus.Published)
            .ToListAsync(cancellationToken);

        foreach (var post in existing)
            _unitOfWork.SocialPosts.Delete(post);

        var drafts = await _contentGenerator.GenerateWeekAsync(tenantId, weekStart, cancellationToken);
        var created = new List<SocialPost>();

        foreach (var draft in drafts)
        {
            var post = new SocialPost
            {
                TenantId = tenantId,
                Topic = draft.Topic,
                Caption = draft.Caption,
                Hashtags = draft.Hashtags,
                ImagePrompt = draft.ImagePrompt,
                ScheduledAt = draft.ScheduledAtUtc,
                Status = SocialPostStatus.Draft,
            };
            await _unitOfWork.SocialPosts.AddAsync(post, cancellationToken);
            created.Add(post);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (_openAiService.ImageGenerationEnabled)
        {
            foreach (var post in created)
            {
                if (string.IsNullOrWhiteSpace(post.ImagePrompt))
                    continue;

                await _imageQueue.EnqueueAsync(
                    new SocialImageJob(tenantId, post.Id, post.ImagePrompt),
                    cancellationToken);
            }
        }

        var igStatus = await _instagramService.GetConnectionStatusAsync(tenantId, cancellationToken);

        return new SocialPostsWeekDto(
            created.OrderBy(p => p.ScheduledAt).Select(p => Queries.GetSocialPosts.SocialPostMapper.ToDto(p, _mediaStorage)).ToList(),
            igStatus.IsConnected,
            igStatus.Username,
            _openAiService.IsConfigured);
    }
}
