using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Parking.Application.Interfaces;
using ParkingSystem.Grpc.Parking;

namespace Parking.WebAPI.GrpcServices;

/// <summary>Answers slot queries from other services (contract: grpc_proto/parking.proto).</summary>
public class ParkingGrpcService(ISlotStateStore slots) : ParkingService.ParkingServiceBase
{
    public override async Task<GetLotSlotsResponse> GetLotSlots(GetLotSlotsRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.LotId, out var lotId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "lot_id must be a GUID"));
        }

        var response = new GetLotSlotsResponse();
        foreach (var slot in await slots.GetByLotAsync(lotId, context.CancellationToken))
        {
            response.Slots.Add(new SlotInfo
            {
                Code = slot.Code,
                Status = slot.Status,
                UpdatedAt = Timestamp.FromDateTimeOffset(slot.UpdatedAt),
            });
        }

        return response;
    }

    // Not implemented on purpose: answering "assigned" without assigning would let every vehicle in.
    // Replace with the Application use case when slot assignment exists (BR-15).
    public override Task<AssignSlotReply> AssignSlot(AssignSlotRequest request, ServerCallContext context) =>
        throw new RpcException(new Status(StatusCode.Unimplemented, "AssignSlot is not implemented yet"));
}
