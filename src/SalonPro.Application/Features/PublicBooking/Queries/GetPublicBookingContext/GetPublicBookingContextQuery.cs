using MediatR;
using SalonPro.Application.Features.PublicBooking.DTOs;

namespace SalonPro.Application.Features.PublicBooking.Queries.GetPublicBookingContext;

public record GetPublicBookingContextQuery(string Slug) : IRequest<PublicBookingContext?>;
