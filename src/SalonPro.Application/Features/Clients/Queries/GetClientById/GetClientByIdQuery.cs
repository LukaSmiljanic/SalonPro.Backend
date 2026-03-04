using MediatR;
using SalonPro.Application.Features.Clients.DTOs;

namespace SalonPro.Application.Features.Clients.Queries.GetClientById;

public record GetClientByIdQuery(Guid Id) : IRequest<ClientDetailDto>;
