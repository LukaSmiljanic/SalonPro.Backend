using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Models;
using SalonPro.Application.Features.Clients.DTOs;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Clients.Queries.GetClients;

public class GetClientsQueryHandler : IRequestHandler<GetClientsQuery, PaginatedList<ClientListDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetClientsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginatedList<ClientListDto>> Handle(GetClientsQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Clients.Query()
            .Include(c => c.Appointments)
                .ThenInclude(a => a.AppointmentServices)
                    .ThenInclude(aps => aps.Service)
                        .ThenInclude(s => s.Category)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(c =>
                c.FirstName.ToLower().Contains(term) ||
                c.LastName.ToLower().Contains(term) ||
                (c.Email != null && c.Email.ToLower().Contains(term)) ||
                c.Phone.Contains(term));
        }

        var projectedQuery = query.Select(c => new ClientListDto(
            c.Id,
            c.FirstName + " " + c.LastName,
            c.Phone,
            c.Email,
            c.Appointments
                .Where(a => a.Status == AppointmentStatus.Completed)
                .OrderByDescending(a => a.StartTime)
                .Select(a => (DateTime?)a.StartTime)
                .FirstOrDefault(),
            c.Appointments
                .Where(a => a.Status == AppointmentStatus.Completed)
                .SelectMany(a => a.AppointmentServices)
                .GroupBy(aps => aps.Service.Name)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault(),
            c.IsVip,
            c.Tags
        ));

        return await PaginatedList<ClientListDto>.CreateAsync(
            projectedQuery,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
