using Booking.Application.Common.Models.Exceptions;
using Booking.Application.DTOs;
using Booking.Application.Usecase.Availability;

namespace Booking.Tests;

public class GetLotAvailabilityQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 29, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_MixedSlots_CountsAvailableAndOccupied()
    {
        var handler = new GetLotAvailabilityQueryHandler(new FakeUnitOfGrpc(
            new SlotStatusDto("A-01", "Available", Now),
            new SlotStatusDto("A-02", "Occupied", Now),
            new SlotStatusDto("A-03", "Occupied", Now)));

        var result = await handler.Handle(new GetLotAvailabilityQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(3, result.Total);
        Assert.Equal(1, result.Available);
        Assert.Equal(2, result.Occupied);
    }

    [Fact]
    public async Task Handle_LotWithoutKnownSlots_ReturnsZeroCounts()
    {
        var handler = new GetLotAvailabilityQueryHandler(new FakeUnitOfGrpc());

        var result = await handler.Handle(new GetLotAvailabilityQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(0, result.Total);
        Assert.Empty(result.Slots);
    }

    [Fact]
    public async Task Handle_ParkingServiceDown_PropagatesParkingUnavailable()
    {
        var handler = new GetLotAvailabilityQueryHandler(new FakeUnitOfGrpc { Failure = new ParkingUnavailableException("down", new Exception()) });

        await Assert.ThrowsAsync<ParkingUnavailableException>(() => handler.Handle(new GetLotAvailabilityQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
