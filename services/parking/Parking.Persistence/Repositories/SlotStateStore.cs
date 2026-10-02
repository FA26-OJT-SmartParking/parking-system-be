using Microsoft.EntityFrameworkCore;
using Parking.Application.Common.Interfaces.Persistence;
using Parking.Domain.Entities;

namespace Parking.Persistence.Repositories;

public class SlotStateStore(ApplicationDbContext db) : ISlotStateStore
{
    public async Task UpsertAsync(Guid lotId, string code, string status, DateTimeOffset at, CancellationToken cancellationToken)
    {
        var slot = await db.SlotStates.FindAsync([lotId, code], cancellationToken);
        if (slot is null)
        {
            db.SlotStates.Add(new SlotState { LotId = lotId, Code = code, Status = status, UpdatedAt = at });
            return;
        }

        // Devices can deliver out of order: keep the newest reading
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
