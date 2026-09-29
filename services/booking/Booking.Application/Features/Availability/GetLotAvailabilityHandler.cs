using Booking.Application.Interfaces;

namespace Booking.Application.Features.Availability;

/// <summary>Counts free and occupied slots of a lot from the parking service.</summary>
public class GetLotAvailabilityHandler(IParkingClient parking)
{
    public async Task<LotAvailability> HandleAsync(Guid lotId, CancellationToken cancellationToken)
    {
        var slots = await parking.GetLotSlotsAsync(lotId, cancellationToken);
        var occupied = slots.Count(slot => slot.Status == "Occupied");
        var available = slots.Count(slot => slot.Status == "Available");
        return new LotAvailability(lotId, slots.Count, available, occupied, slots);
    }
}
