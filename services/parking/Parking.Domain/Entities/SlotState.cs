namespace Parking.Domain.Entities;

/// <summary>Latest known status of one slot, kept up to date from device readings.</summary>
public class SlotState
{
    public Guid LotId { get; set; }

    /// <summary>Slot code inside the lot, for example "A-01".</summary>
    public string Code { get; set; } = "";

    /// <summary>"Available" or "Occupied".</summary>
    public string Status { get; set; } = "";

    public DateTimeOffset UpdatedAt { get; set; }
}
