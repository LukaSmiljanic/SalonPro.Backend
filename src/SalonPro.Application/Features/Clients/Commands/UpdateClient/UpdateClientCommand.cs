using MediatR;

namespace SalonPro.Application.Features.Clients.Commands.UpdateClient;

public record UpdateClientCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string Phone,
    string? Notes,
    bool IsVip,
    string? Tags
) : IRequest<Unit>;
