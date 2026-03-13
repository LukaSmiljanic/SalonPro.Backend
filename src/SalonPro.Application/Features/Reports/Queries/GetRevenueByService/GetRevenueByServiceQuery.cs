using MediatR;
using SalonPro.Application.Features.Reports.DTOs;

namespace SalonPro.Application.Features.Reports.Queries.GetRevenueByService;

public record GetRevenueByServiceQuery(DateTime From, DateTime To) : IRequest<List<ServiceRevenueDto>>;
