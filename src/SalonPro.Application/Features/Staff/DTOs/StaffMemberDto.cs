namespace SalonPro.Application.Features.Staff.DTOs;

public record StaffMemberDto(
    Guid Id,
    string FullName,
    string? Specialization,
    string? Email,
    string? Phone,
    bool IsActive,
    int AppointmentCountToday
);
