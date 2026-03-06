using MediatR;

namespace SalonPro.Application.Features.Clients.Commands.CreateClient;

public record CreateClientCommand(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    DateTime? DateOfBirth,
    string? Notes,
    bool IsVip = false,
    string? Tags = null
) : IRequest<Guid>;
