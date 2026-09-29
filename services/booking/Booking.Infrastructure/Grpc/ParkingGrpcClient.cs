using Booking.Application.Exceptions;
using Booking.Application.Features.Availability;
using Booking.Application.Interfaces;
using Grpc.Core;
using ParkingSystem.Grpc.Parking;

namespace Booking.Infrastructure.Grpc;

/// <summary>Calls the parking service over gRPC (contract: grpc_proto/parking.proto).</summary>
public class ParkingGrpcClient(ParkingService.ParkingServiceClient client) : IParkingClient
{
    public async Task<IReadOnlyList<SlotStatus>> GetLotSlotsAsync(Guid lotId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.GetLotSlotsAsync(
                new GetLotSlotsRequest { LotId = lotId.ToString() },
                cancellationToken: cancellationToken);
            return response.Slots
                .Select(slot => new SlotStatus(slot.Code, slot.Status, slot.UpdatedAt.ToDateTimeOffset()))
                .ToList();
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            throw new ParkingUnavailableException("The parking service is not reachable.", ex);
        }
    }
}
