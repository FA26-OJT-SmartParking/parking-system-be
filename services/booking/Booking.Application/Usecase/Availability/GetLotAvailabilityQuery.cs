using Booking.Application.DTOs;
using MediatR;

namespace Booking.Application.Usecase.Availability;

/// <summary>Free and occupied slots of a lot.</summary>
public record GetLotAvailabilityQuery(Guid LotId) : IRequest<LotAvailabilityDto>;
