using MediatR;
using SalonPro.Application.Common.Models;
using SalonPro.Application.Features.Clients.DTOs;

namespace SalonPro.Application.Features.Clients.Queries.GetClients;

public record GetClientsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    bool IncludeInactive = false
) : IRequest<PaginatedList<ClientListDto>>;
