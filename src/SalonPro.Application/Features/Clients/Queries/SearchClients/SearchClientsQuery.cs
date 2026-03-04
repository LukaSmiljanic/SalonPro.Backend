using MediatR;
using SalonPro.Application.Features.Clients.DTOs;

namespace SalonPro.Application.Features.Clients.Queries.SearchClients;

public record SearchClientsQuery(string SearchTerm) : IRequest<List<ClientListDto>>;
