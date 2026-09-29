using Booking.Application.Features.Availability;
using Booking.Infrastructure.Grpc;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Parking.API.GrpcServices;
using Parking.Application.Interfaces;
using Parking.Domain;
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

        var availability = await new GetLotAvailabilityHandler(bookingSide).HandleAsync(LotId, CancellationToken.None);

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

    private static SlotState Slot(string code, string status) =>
        new() { LotId = LotId, Code = code, Status = status, UpdatedAt = DateTimeOffset.UtcNow };

    private static async Task<RunningParkingService> StartParkingAsync(params SlotState[] slots)
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddGrpc();
                    services.AddSingleton<ISlotStateStore>(new FakeSlotStateStore(slots));
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapGrpcService<ParkingGrpcService>());
                }))
            .StartAsync();

        var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = host.GetTestServer().CreateHandler(),
        });
        return new RunningParkingService(host, channel);
    }

    private sealed class RunningParkingService(IHost host, GrpcChannel channel) : IAsyncDisposable
    {
        public GrpcChannel Channel { get; } = channel;

        public async ValueTask DisposeAsync()
        {
            Channel.Dispose();
            await host.StopAsync();
            host.Dispose();
        }
    }

    private sealed class FakeSlotStateStore(SlotState[] slots) : ISlotStateStore
    {
        public Task UpsertAsync(Guid lotId, string code, string status, DateTimeOffset at, CancellationToken cancellationToken) =>
            throw new NotSupportedException("The gRPC service only reads slots");

        public Task<IReadOnlyList<SlotState>> GetByLotAsync(Guid lotId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SlotState>>(slots.Where(slot => slot.LotId == lotId).OrderBy(slot => slot.Code).ToList());
    }
}
