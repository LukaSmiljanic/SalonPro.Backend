using MediatR;
using SalonPro.Application.Common.Interfaces;

namespace SalonPro.Application.Features.Social.Commands.CompleteInstagramOAuth;

public record CompleteInstagramOAuthCommand(Guid TenantId, string Code) : IRequest<Unit>;

public class CompleteInstagramOAuthCommandHandler : IRequestHandler<CompleteInstagramOAuthCommand, Unit>
{
    private readonly IInstagramService _instagramService;

    public CompleteInstagramOAuthCommandHandler(IInstagramService instagramService)
    {
        _instagramService = instagramService;
    }

    public async Task<Unit> Handle(CompleteInstagramOAuthCommand request, CancellationToken cancellationToken)
    {
        await _instagramService.CompleteOAuthAsync(request.TenantId, request.Code, cancellationToken);
        return Unit.Value;
    }
}
