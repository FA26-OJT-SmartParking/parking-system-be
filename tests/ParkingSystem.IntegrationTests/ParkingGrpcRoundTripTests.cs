using Booking.Application.Common.Interfaces.Grpc;
using Booking.Application.Common.Models.Exceptions;
using Booking.Application.Usecase.Availability;
using Booking.Infrastructure.GRPC;
using Booking.Infrastructure.GRPC.Client;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Parking.Infrastructure.GRPC.Services;
using Parking.Application.Common.Interfaces.Persistence;
using Parking.Domain.Entities;
using ParkingSystem.Grpc.Parking;

namespace ParkingSystem.IntegrationTests;

/// <summary>
/// Real gRPC over an in-memory server: the booking client adapter calls the parking gRPC service.
/// Only the slot store is faked, so no database or Docker is needed.
/// </summary>
public class ParkingGrpcRoundTripTests
{
    private static readonly Guid LotId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task GetLotAvailability_BookingCallsParkingOverGrpc_CountsSlotsByStatus()
    {
        await using var parking = await StartParkingAsync(
            Slot("A-01", "Occupied"), Slot("A-02", "Available"), Slot("A-03", "Available"));
        var bookingSide = new ParkingGrpcClient(new ParkingService.ParkingServiceClient(parking.Channel));

        var availability = await new GetLotAvailabilityQueryHandler(new UnitOfGrpc(bookingSide))
            .Handle(new GetLotAvailabilityQuery(LotId), CancellationToken.None);

        Assert.Equal(3, availability.Total);
        Assert.Equal(2, availability.Available);
        Assert.Equal(1, availability.Occupied);
        Assert.Equal(["A-01", "A-02", "A-03"], availability.Slots.Select(slot => slot.Code));
    }

    [Fact]
    public async Task GetLotSlots_LotIdIsNotAGuid_ReturnsInvalidArgument()
    {
        await using var parking = await StartParkingAsync();
        var client = new ParkingService.ParkingServiceClient(parking.Channel);

        var error = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.GetLotSlotsAsync(new GetLotSlotsRequest { LotId = "not-a-guid" }));

        Assert.Equal(StatusCode.InvalidArgument, error.StatusCode);
    }

    [Fact]
    public async Task AssignSlot_NotImplementedYet_ServerAnswersUnimplemented()
    {
        await using var parking = await StartParkingAsync();
        var client = new ParkingService.ParkingServiceClient(parking.Channel);

        var error = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.AssignSlotAsync(new AssignSlotRequest { LotId = LotId.ToString(), ReservationId = Guid.NewGuid().ToString(), VehicleType = "Car" }));

        Assert.Equal(StatusCode.Unimplemented, error.StatusCode);
    }

    [Fact]
    public async Task AssignSlotAsync_NotImplementedYet_ClientReportsNotImplementedInsteadOfAssigning()
    {
        await using var parking = await StartParkingAsync();
        var bookingSide = new ParkingGrpcClient(new ParkingService.ParkingServiceClient(parking.Channel));

        await Assert.ThrowsAsync<RemoteCallNotImplementedException>(() =>
            bookingSide.AssignSlotAsync(Guid.NewGuid(), LotId, "Car", CancellationToken.None));
    }

    private static SlotState Slot(string code, string status) =>
        new() { LotId = LotId, Code = code, Status = status, UpdatedAt = DateTimeOffset.UtcNow };

    private static Task<GrpcTestHost> StartParkingAsync(params SlotState[] slots) =>
        GrpcTestHost.StartAsync<ParkingGrpcService>(services => services.AddSingleton<ISlotStateStore>(new FakeSlotStateStore(slots)));

    private sealed class FakeSlotStateStore(SlotState[] slots) : ISlotStateStore
    {
        public Task UpsertAsync(Guid lotId, string code, string status, DateTimeOffset at, CancellationToken cancellationToken) =>
            throw new NotSupportedException("The gRPC service only reads slots");

        public Task<IReadOnlyList<SlotState>> GetByLotAsync(Guid lotId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SlotState>>(slots.Where(slot => slot.LotId == lotId).OrderBy(slot => slot.Code).ToList());
    }
}
