using MediatR;
using SalonPro.Application.Features.Settings.DTOs;

namespace SalonPro.Application.Features.Settings.Queries.GetWorkingHours;

public record GetWorkingHoursQuery() : IRequest<List<TenantWorkingHoursDto>>;
