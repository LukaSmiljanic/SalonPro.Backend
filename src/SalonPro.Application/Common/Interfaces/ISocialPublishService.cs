using SalonPro.Domain.Entities;

namespace SalonPro.Application.Common.Interfaces;

public interface ISocialPublishService
{
    Task PublishDuePostAsync(SocialPost post, CancellationToken cancellationToken = default);
}
