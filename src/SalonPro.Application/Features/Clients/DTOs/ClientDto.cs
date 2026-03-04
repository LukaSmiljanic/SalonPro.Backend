namespace SalonPro.Application.Features.Clients.DTOs;

public record ClientDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string? Email,
    string Phone,
    string? Notes,
    bool IsVip,
    string? Tags
);
