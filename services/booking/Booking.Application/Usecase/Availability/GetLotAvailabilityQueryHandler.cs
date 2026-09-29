using Booking.Application.Common.Interfaces.Grpc;
using Booking.Application.DTOs;
using MediatR;

namespace Booking.Application.Usecase.Availability;

/// <summary>Counts free and occupied slots of a lot from the parking service.</summary>
public class GetLotAvailabilityQueryHandler(IUnitOfGrpc unitOfGrpc) : IRequestHandler<GetLotAvailabilityQuery, LotAvailabilityDto>
{
    public async Task<LotAvailabilityDto> Handle(GetLotAvailabilityQuery request, CancellationToken cancellationToken)
    {
        var slots = await unitOfGrpc.ParkingGrpcClient.GetLotSlotsAsync(request.LotId, cancellationToken);
        var occupied = slots.Count(slot => slot.Status == "Occupied");
        var available = slots.Count(slot => slot.Status == "Available");
        return new LotAvailabilityDto(request.LotId, slots.Count, available, occupied, slots);
    }
}
