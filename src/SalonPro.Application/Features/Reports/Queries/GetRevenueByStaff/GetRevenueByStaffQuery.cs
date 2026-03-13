using MediatR;
using SalonPro.Application.Features.Reports.DTOs;

namespace SalonPro.Application.Features.Reports.Queries.GetRevenueByStaff;

public record GetRevenueByStaffQuery(DateTime From, DateTime To) : IRequest<List<StaffRevenueDto>>;
