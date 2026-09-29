using Booking.Application.Common.Interfaces.Grpc;
using Booking.Application.Common.Models.Exceptions;
using Booking.Application.DTOs;
using Grpc.Core;
using ParkingSystem.Grpc.Parking;

namespace Booking.Infrastructure.GRPC.Client;

/// <summary>Calls the parking service over gRPC (contract: grpc_proto/parking.proto).</summary>
public class ParkingGrpcClient(ParkingService.ParkingServiceClient client) : IParkingGrpcClient
{
    public async Task<IReadOnlyList<SlotStatusDto>> GetLotSlotsAsync(Guid lotId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.GetLotSlotsAsync(
                new GetLotSlotsRequest { LotId = lotId.ToString() },
                cancellationToken: cancellationToken);
            return response.Slots
                .Select(slot => new SlotStatusDto(slot.Code, slot.Status, slot.UpdatedAt.ToDateTimeOffset()))
                .ToList();
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            throw new ParkingUnavailableException(Application.Resources.ParkingServiceUnavailable, ex);
        }
    }

    public async Task<(bool Success, string SlotCode, string ErrorCode)> AssignSlotAsync(
        Guid reservationId,
        Guid lotId,
        string vehicleType,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.AssignSlotAsync(new AssignSlotRequest
            {
                ReservationId = reservationId.ToString(),
                LotId = lotId.ToString(),
                VehicleType = vehicleType
            }, cancellationToken: cancellationToken);
            return (response.Success, response.SlotCode, response.ErrorCode);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unimplemented)
        {
            throw new RemoteCallNotImplementedException("The parking service has not implemented AssignSlot yet.", ex);
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            throw new ParkingUnavailableException(Application.Resources.ParkingServiceUnavailable, ex);
        }
    }
}
