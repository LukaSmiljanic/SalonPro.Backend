using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;
using AppointmentServiceEntity = SalonPro.Domain.Entities.AppointmentService;

namespace SalonPro.Application.Features.Appointments.Commands.UpdateAppointment;

public class UpdateAppointmentCommandHandler : IRequestHandler<UpdateAppointmentCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAppointmentCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _unitOfWork.Appointments.Query()
            .Include(a => a.AppointmentServices)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), request.Id);

        var client = await _unitOfWork.Clients.GetByIdAsync(request.ClientId, cancellationToken)
            ?? throw new NotFoundException(nameof(Client), request.ClientId);
        if (!client.IsActive)
            throw new ValidationException("Izabrani klijent je deaktiviran i ne može se koristiti za zakazivanje.");

        var staffMember = await _unitOfWork.StaffMembers.GetByIdAsync(request.StaffMemberId, cancellationToken)
            ?? throw new NotFoundException(nameof(StaffMember), request.StaffMemberId);

        var services = await _unitOfWork.Services.Query()
            .Where(s => request.ServiceIds.Contains(s.Id) && s.IsActive)
            .ToListAsync(cancellationToken);

        if (services.Count != request.ServiceIds.Count)
        {
            throw new NotFoundException("Jedna ili više usluga nisu pronađene ili su neaktivne.");
        }

        foreach (var existingService in appointment.AppointmentServices.ToList())
        {
            _unitOfWork.AppointmentServices.Remove(existingService);
        }

        var totalDuration = services.Sum(s => s.DurationMinutes);
        var totalPrice = services.Sum(s => s.Price);
        var newEndTime = request.StartTime.AddMinutes(totalDuration);

        var hasConflict = await _unitOfWork.Appointments.Query()
            .AnyAsync(a =>
                a.TenantId == appointment.TenantId &&
                a.Id != appointment.Id &&
                a.StaffMemberId == request.StaffMemberId &&
                a.Status != AppointmentStatus.Cancelled &&
                request.StartTime < a.EndTime &&
                newEndTime > a.StartTime,
                cancellationToken);

        if (hasConflict)
        {
            throw new ValidationException("Izabrani zaposleni već ima termin u tom vremenskom periodu.");
        }

        appointment.ClientId = request.ClientId;
        appointment.StaffMemberId = request.StaffMemberId;
        appointment.StartTime = request.StartTime;
        appointment.EndTime = newEndTime;
        appointment.TotalPrice = totalPrice;
        appointment.Notes = request.Notes;
        appointment.Status = request.Status;
        appointment.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Appointments.Update(appointment);

        foreach (var service in services)
        {
            var appointmentService = new AppointmentServiceEntity
            {
                AppointmentId = appointment.Id,
                ServiceId = service.Id,
                Price = service.Price,
                DurationMinutes = service.DurationMinutes
            };
            await _unitOfWork.AppointmentServices.AddAsync(appointmentService, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
