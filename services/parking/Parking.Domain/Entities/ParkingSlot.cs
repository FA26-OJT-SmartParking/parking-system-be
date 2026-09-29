using Parking.Domain.Enum;

namespace Parking.Domain.Entities;

public sealed class ParkingSlot
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid LotId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public SlotStatus Status { get; private set; } = SlotStatus.Available;

    private ParkingSlot() { }

    public ParkingSlot(Guid lotId, string code)
    {
        LotId = lotId;
        Code = code;
    }

    public void UpdateStatus(SlotStatus status) => Status = status;
}
