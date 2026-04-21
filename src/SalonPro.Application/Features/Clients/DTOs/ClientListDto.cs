namespace SalonPro.Application.Features.Clients.DTOs;

public record ClientListDto(
    Guid Id,
    string FullName,
    string Phone,
    string? Email,
    bool IsActive,
    DateTime? LastVisitDate,
    string? FavoriteService,
    bool IsVip,
    string? Tags,
    int TotalVisits
);
