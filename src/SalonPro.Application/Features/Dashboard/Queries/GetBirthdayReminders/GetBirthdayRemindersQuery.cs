using MediatR;
using SalonPro.Application.Features.Dashboard.DTOs;

namespace SalonPro.Application.Features.Dashboard.Queries.GetBirthdayReminders;

public record GetBirthdayRemindersQuery(int Days = 7) : IRequest<List<BirthdayReminderDto>>;
