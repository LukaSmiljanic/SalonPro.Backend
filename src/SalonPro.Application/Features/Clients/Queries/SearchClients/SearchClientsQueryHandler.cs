using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Clients.DTOs;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Clients.Queries.SearchClients;

public class SearchClientsQueryHandler : IRequestHandler<SearchClientsQuery, List<ClientListDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public SearchClientsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ClientListDto>> Handle(SearchClientsQuery request, CancellationToken cancellationToken)
    {
        var term = request.SearchTerm.ToLower();

        var clients = await _unitOfWork.Clients.Query()
            .Include(c => c.Appointments)
                .ThenInclude(a => a.AppointmentServices)
                    .ThenInclude(aps => aps.Service)
            .Where(c =>
                c.FirstName.ToLower().Contains(term) ||
                c.LastName.ToLower().Contains(term) ||
                (c.Email != null && c.Email.ToLower().Contains(term)) ||
                c.Phone.Contains(term))
            .AsNoTracking()
            .Take(50)
            .ToListAsync(cancellationToken);

        return clients.Select(c => new ClientListDto(
            c.Id,
            c.FullName,
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
        )).ToList();
    }
}
