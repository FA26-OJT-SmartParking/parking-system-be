using Booking.Application.DTOs;

namespace Booking.Application.Common.Interfaces.Grpc;

/// <summary>Reads and changes slot data owned by the parking service (contract: grpc_proto/parking.proto).</summary>
public interface IParkingGrpcClient
{
    /// <exception cref="Models.Exceptions.ParkingUnavailableException">The parking service cannot be reached.</exception>
    Task<IReadOnlyList<SlotStatusDto>> GetLotSlotsAsync(Guid lotId, CancellationToken cancellationToken);

    /// <exception cref="Models.Exceptions.RemoteCallNotImplementedException">The parking service has not implemented AssignSlot yet.</exception>
    Task<(bool Success, string SlotCode, string ErrorCode)> AssignSlotAsync(Guid reservationId, Guid lotId, string vehicleType, CancellationToken cancellationToken);
}
