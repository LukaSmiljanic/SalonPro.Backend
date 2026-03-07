using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Dashboard.DTOs;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Dashboard.Queries.GetBirthdayReminders;

public class GetBirthdayRemindersQueryHandler : IRequestHandler<GetBirthdayRemindersQuery, List<BirthdayReminderDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;

    public GetBirthdayRemindersQueryHandler(IUnitOfWork unitOfWork, ICurrentTenantService currentTenantService)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
    }

    public async Task<List<BirthdayReminderDto>> Handle(GetBirthdayRemindersQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Tenant ID is required.");

        var today = DateTime.UtcNow.Date;
        var days = Math.Max(1, Math.Min(request.Days, 30));

        // Get all active clients with DateOfBirth set, scoped to tenant
        var clients = await _unitOfWork.Clients.Query()
            .Where(c => c.TenantId == tenantId && c.IsActive && c.DateOfBirth.HasValue)
            .AsNoTracking()
            .Select(c => new
            {
                c.Id,
                FullName = c.FirstName + " " + c.LastName,
                c.Phone,
                c.Email,
                DateOfBirth = c.DateOfBirth!.Value
            })
            .ToListAsync(cancellationToken);

        // Calculate days until birthday in memory (date logic too complex for EF translation)
        var reminders = new List<BirthdayReminderDto>();

        foreach (var client in clients)
        {
            var birthdayThisYear = new DateTime(today.Year, client.DateOfBirth.Month, client.DateOfBirth.Day);
            if (birthdayThisYear < today)
            {
                birthdayThisYear = birthdayThisYear.AddYears(1);
            }

            var daysUntil = (birthdayThisYear - today).Days;

            if (daysUntil <= days)
            {
                var age = birthdayThisYear.Year - client.DateOfBirth.Year;
                reminders.Add(new BirthdayReminderDto(
                    client.Id,
                    client.FullName,
                    client.Phone,
                    client.Email,
                    client.DateOfBirth,
                    daysUntil,
                    age
                ));
            }
        }

        return reminders.OrderBy(r => r.DaysUntilBirthday).ToList();
    }
}
