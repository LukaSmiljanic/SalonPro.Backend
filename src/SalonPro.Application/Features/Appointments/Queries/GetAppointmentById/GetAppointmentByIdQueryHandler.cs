using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Features.Appointments.DTOs;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Appointments.Queries.GetAppointmentById;

public class GetAppointmentByIdQueryHandler : IRequestHandler<GetAppointmentByIdQuery, AppointmentDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAppointmentByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AppointmentDetailDto> Handle(GetAppointmentByIdQuery request, CancellationToken cancellationToken)
    {
        var appointment = await _unitOfWork.Appointments.Query()
            .Include(a => a.Client)
            .Include(a => a.StaffMember)
            .Include(a => a.AppointmentServices)
                .ThenInclude(aps => aps.Service)
                    .ThenInclude(s => s.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Appointment), request.Id);

        return new AppointmentDetailDto(
            appointment.Id,
            appointment.ClientId,
            appointment.Client.FullName,
            appointment.StaffMemberId,
            appointment.StaffMember.FullName,
            appointment.StartTime,
            appointment.EndTime,
            appointment.Status,
            appointment.TotalPrice,
            appointment.Notes,
            appointment.CancellationReason,
            appointment.AppointmentServices.Select(aps => new AppointmentServiceDto(
                aps.ServiceId,
                aps.Service.Name,
                aps.Price,
                aps.DurationMinutes
            )).ToList()
        );
    }
}
