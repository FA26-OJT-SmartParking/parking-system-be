using Microsoft.EntityFrameworkCore;
using Parking.Application.Interfaces;
using Parking.Domain;

namespace Parking.Infrastructure.Persistence;

public class SlotStateStore(ParkingDb db) : ISlotStateStore
{
    public async Task UpsertAsync(Guid lotId, string code, string status, DateTimeOffset at, CancellationToken cancellationToken)
    {
        var slot = await db.SlotStates.FindAsync([lotId, code], cancellationToken);
        if (slot is null)
        {
            db.SlotStates.Add(new SlotState { LotId = lotId, Code = code, Status = status, UpdatedAt = at });
            return;
        }

        // Cameras can deliver out of order: keep the newest reading
        if (at >= slot.UpdatedAt)
        {
            slot.Status = status;
            slot.UpdatedAt = at;
        }
    }

    public async Task<IReadOnlyList<SlotState>> GetByLotAsync(Guid lotId, CancellationToken cancellationToken) =>
        await db.SlotStates.AsNoTracking()
            .Where(s => s.LotId == lotId)
            .OrderBy(s => s.Code)
            .ToListAsync(cancellationToken);
}
