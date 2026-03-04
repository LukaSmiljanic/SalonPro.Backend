using MediatR;

namespace SalonPro.Application.Features.Clients.Commands.DeleteClient;

public record DeleteClientCommand(Guid Id) : IRequest<Unit>;
