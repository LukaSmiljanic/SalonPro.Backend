using MediatR;

namespace SalonPro.Application.Features.Services.Commands.CreateService;

public record CreateServiceCommand(
    Guid CategoryId,
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price
) : IRequest<Guid>;
