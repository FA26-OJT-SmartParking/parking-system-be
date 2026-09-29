using Booking.Application.Exceptions;
using Booking.Application.Features.Availability;
using Booking.Application.Interfaces;

namespace Booking.Tests;

public class GetLotAvailabilityHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 29, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_MixedSlots_CountsAvailableAndOccupied()
    {
        var handler = new GetLotAvailabilityHandler(new FakeParkingClient(
            new SlotStatus("A-01", "Available", Now),
            new SlotStatus("A-02", "Occupied", Now),
            new SlotStatus("A-03", "Occupied", Now)));

        var result = await handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(3, result.Total);
        Assert.Equal(1, result.Available);
        Assert.Equal(2, result.Occupied);
    }

    [Fact]
    public async Task HandleAsync_LotWithoutKnownSlots_ReturnsZeroCounts()
    {
        var handler = new GetLotAvailabilityHandler(new FakeParkingClient());

        var result = await handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(0, result.Total);
        Assert.Empty(result.Slots);
    }

    [Fact]
    public async Task HandleAsync_ParkingServiceDown_PropagatesParkingUnavailable()
    {
        var handler = new GetLotAvailabilityHandler(new FakeParkingClient { Failure = new ParkingUnavailableException("down", new Exception()) });

        await Assert.ThrowsAsync<ParkingUnavailableException>(() => handler.HandleAsync(Guid.NewGuid(), CancellationToken.None));
    }

    private sealed class FakeParkingClient(params SlotStatus[] slots) : IParkingClient
    {
        public Exception? Failure { get; init; }

        public Task<IReadOnlyList<SlotStatus>> GetLotSlotsAsync(Guid lotId, CancellationToken cancellationToken) =>
            Failure is null ? Task.FromResult<IReadOnlyList<SlotStatus>>(slots) : Task.FromException<IReadOnlyList<SlotStatus>>(Failure);
    }
}
