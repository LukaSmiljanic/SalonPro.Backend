using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Features.Clients.DTOs;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Clients.Queries.GetClientById;

public class GetClientByIdQueryHandler : IRequestHandler<GetClientByIdQuery, ClientDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetClientByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ClientDetailDto> Handle(GetClientByIdQuery request, CancellationToken cancellationToken)
    {
        var client = await _unitOfWork.Clients.Query()
            .Include(c => c.Appointments)
                .ThenInclude(a => a.AppointmentServices)
                    .ThenInclude(aps => aps.Service)
            .Include(c => c.Appointments)
                .ThenInclude(a => a.StaffMember)
            .Include(c => c.ClientNotes)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Client), request.Id);

        var completedAppointments = client.Appointments
            .Where(a => a.Status == AppointmentStatus.Completed)
            .OrderByDescending(a => a.StartTime)
            .ToList();

        var totalVisits = completedAppointments.Count;
        var totalSpent = completedAppointments.Sum(a => a.TotalPrice);
        var lastVisitDate = completedAppointments.FirstOrDefault()?.StartTime;

        var visitHistory = completedAppointments.Select(a => new VisitHistoryDto(
            a.StartTime,
            string.Join(", ", a.AppointmentServices.Select(aps => aps.Service.Name)),
            a.StaffMember.FullName,
            a.TotalPrice
        )).ToList();

        var notes = client.ClientNotes
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new ClientNoteDto(n.Id, n.Content, n.CreatedAt, n.CreatedBy))
            .ToList();

        return new ClientDetailDto(
            client.Id,
            client.FirstName,
            client.LastName,
            client.FullName,
            client.Email,
            client.Phone,
            client.Notes,
            client.IsVip,
            client.Tags,
            totalVisits,
            totalSpent,
            lastVisitDate,
            visitHistory,
            notes
        );
    }
}
