using MediatR;
using SalonPro.Application.Features.Tenants.DTOs;

namespace SalonPro.Application.Features.Tenants.Queries.GetTenants;

public record GetTenantsQuery() : IRequest<List<TenantListDto>>;
