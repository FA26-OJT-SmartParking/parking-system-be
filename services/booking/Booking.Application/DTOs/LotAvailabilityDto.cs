namespace Booking.Application.DTOs;

public record LotAvailabilityDto(Guid LotId, int Total, int Available, int Occupied, IReadOnlyList<SlotStatusDto> Slots);
