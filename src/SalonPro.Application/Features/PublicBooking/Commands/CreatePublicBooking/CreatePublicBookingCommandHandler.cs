using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Features.Appointments.Commands.CreateAppointment;
using SalonPro.Application.Features.Clients.Commands.CreateClient;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.PublicBooking.Commands.CreatePublicBooking;

public class CreatePublicBookingCommandHandler : IRequestHandler<CreatePublicBookingCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ISender _sender;

    public CreatePublicBookingCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService,
        ISender sender)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
        _sender = sender;
    }

    public async Task<Guid> Handle(CreatePublicBookingCommand request, CancellationToken cancellationToken)
    {
        _ = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var normalized = NormalizePhoneDigits(request.Phone);
        if (string.IsNullOrEmpty(normalized))
            throw new ValidationException("Unesite validan broj telefona.");

        var clients = await _unitOfWork.Clients.Query()
            .AsNoTracking()
            .Where(c => c.Phone != null)
            .Select(c => new { c.Id, c.Phone })
            .ToListAsync(cancellationToken);

        Guid clientId;
        var match = clients.FirstOrDefault(c =>
            c.Phone != null && NormalizePhoneDigits(c.Phone) == normalized);

        if (match != null)
        {
            clientId = match.Id;
        }
        else
        {
            clientId = await _sender.Send(
                new CreateClientCommand(
                    request.FirstName.Trim(),
                    request.LastName.Trim(),
                    string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                    request.Phone.Trim(),
                    null,
                    "Kreirano putem online zakazivanja.",
                    false,
                    null),
                cancellationToken);
        }

        return await _sender.Send(
            new CreateAppointmentCommand(
                clientId,
                request.StaffMemberId,
                request.StartTime,
                request.ServiceIds,
                string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()),
            cancellationToken);
    }

    private static string NormalizePhoneDigits(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
        return new string(phone.Where(char.IsDigit).ToArray());
    }
}
