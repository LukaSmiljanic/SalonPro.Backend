using MediatR;
using SalonPro.Application.Features.Clients.DTOs;

namespace SalonPro.Application.Features.Clients.Queries.GetClientLoyalty;

public record GetClientLoyaltyQuery(Guid ClientId) : IRequest<ClientLoyaltyDto>;
