namespace SalonPro.Application.Features.Social.DTOs;

public record RegenerateImageQueuedDto(
    Guid PostId,
    string Status,
    string Message);
