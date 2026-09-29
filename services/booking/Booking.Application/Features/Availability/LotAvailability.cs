namespace Booking.Application.Features.Availability;

public record SlotStatus(string Code, string Status, DateTimeOffset UpdatedAt);

public record LotAvailability(Guid LotId, int Total, int Available, int Occupied, IReadOnlyList<SlotStatus> Slots);
