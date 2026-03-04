using MediatR;
using SalonPro.Application.Features.Services.DTOs;

namespace SalonPro.Application.Features.Services.Queries.GetServicesByCategory;

public record GetServicesByCategoryQuery(Guid CategoryId) : IRequest<List<ServiceDto>>;
