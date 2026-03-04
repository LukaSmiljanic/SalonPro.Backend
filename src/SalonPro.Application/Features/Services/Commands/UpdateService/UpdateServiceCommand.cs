using MediatR;

namespace SalonPro.Application.Features.Services.Commands.UpdateService;

public record UpdateServiceCommand(
    Guid Id,
    Guid CategoryId,
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    bool IsActive
) : IRequest<Unit>;
