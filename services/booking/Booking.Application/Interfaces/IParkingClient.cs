using Booking.Application.Features.Availability;

namespace Booking.Application.Interfaces;

/// <summary>Reads slot data owned by the parking service.</summary>
public interface IParkingClient
{
    /// <exception cref="Exceptions.ParkingUnavailableException">The parking service cannot be reached.</exception>
    Task<IReadOnlyList<SlotStatus>> GetLotSlotsAsync(Guid lotId, CancellationToken cancellationToken);

    Task<(bool Success, string SlotCode, string ErrorCode)> AssignSlotAsync(Guid reservationId, Guid lotId, string vehicleType, CancellationToken cancellationToken);
}
