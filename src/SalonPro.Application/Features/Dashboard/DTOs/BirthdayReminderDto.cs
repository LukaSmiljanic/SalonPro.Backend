namespace SalonPro.Application.Features.Dashboard.DTOs;

public record BirthdayReminderDto(
    Guid ClientId,
    string FullName,
    string? Phone,
    string? Email,
    DateTime DateOfBirth,
    int DaysUntilBirthday,
    int Age
);
