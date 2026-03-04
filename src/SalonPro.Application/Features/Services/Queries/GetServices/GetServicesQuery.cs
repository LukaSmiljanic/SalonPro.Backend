using MediatR;
using SalonPro.Application.Features.Services.DTOs;

namespace SalonPro.Application.Features.Services.Queries.GetServices;

public record GetServicesQuery(bool IncludeInactive = false) : IRequest<List<ServiceDto>>;
