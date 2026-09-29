using Parking.Domain;

namespace Parking.Application.Interfaces;

public interface ISlotStateStore
{
    /// <summary>
    /// Adds or updates a slot. Nothing is saved here: the caller commits with SaveChanges,
    /// so the state and any outbox message are written in one transaction.
    /// </summary>
    Task UpsertAsync(Guid lotId, string code, string status, DateTimeOffset at, CancellationToken cancellationToken);

    /// <summary>Every known slot of a lot, ordered by code.</summary>
    Task<IReadOnlyList<SlotState>> GetByLotAsync(Guid lotId, CancellationToken cancellationToken);
}
