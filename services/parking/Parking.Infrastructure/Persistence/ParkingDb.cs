using MassTransit;
using Microsoft.EntityFrameworkCore;
using Parking.Domain;

namespace Parking.Infrastructure.Persistence;

public class ParkingDb(DbContextOptions<ParkingDb> options) : DbContext(options)
{
    public DbSet<SlotState> SlotStates => Set<SlotState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SlotState>(slot =>
        {
            slot.HasKey(s => new { s.LotId, s.Code });
            slot.Property(s => s.Code).HasMaxLength(32);
            slot.Property(s => s.Status).HasMaxLength(32);
        });

        // Tables used by the MassTransit outbox (publish) and inbox (consume)
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
