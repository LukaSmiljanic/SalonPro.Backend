using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;
using AppointmentServiceEntity = SalonPro.Domain.Entities.AppointmentService;

namespace SalonPro.Application.Features.Appointments.Commands.CreateAppointment;

public class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;

    public CreateAppointmentCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
    }

    public async Task<Guid> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Tenant context is not set.");

        var client = await _unitOfWork.Clients.GetByIdAsync(request.ClientId, cancellationToken)
            ?? throw new NotFoundException(nameof(Client), request.ClientId);

        var staffMember = await _unitOfWork.StaffMembers.GetByIdAsync(request.StaffMemberId, cancellationToken)
            ?? throw new NotFoundException(nameof(StaffMember), request.StaffMemberId);

        var services = await _unitOfWork.Services.Query()
            .Where(s => request.ServiceIds.Contains(s.Id) && s.IsActive)
            .ToListAsync(cancellationToken);

        if (services.Count != request.ServiceIds.Count())
        {
            throw new NotFoundException("One or more services were not found or are inactive.");
        }

        var totalDuration = services.Sum(s => s.DurationMinutes);
        var totalPrice = services.Sum(s => s.Price);
        var endTime = request.StartTime.AddMinutes(totalDuration);

        var appointment = new Appointment
        {
            TenantId = tenantId,
            ClientId = request.ClientId,
            StaffMemberId = request.StaffMemberId,
            StartTime = request.StartTime,
            EndTime = endTime,
            Status = AppointmentStatus.Scheduled,
            TotalPrice = totalPrice,
            TotalDurationMinutes = totalDuration,
            Notes = request.Notes
        };

        await _unitOfWork.Appointments.AddAsync(appointment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

        return appointment.Id;
    }
}
