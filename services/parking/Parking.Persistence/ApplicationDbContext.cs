using System.Linq.Expressions;
using Parking.Domain.Base;
using Parking.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Parking.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<SlotState> SlotStates => Set<SlotState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Soft delete: rows flagged as deleted are hidden from every query; use QueryIncludingDeleted() to see them.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(e => typeof(ISoftDelete).IsAssignableFrom(e.ClrType)).ToList())
        {
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var isDeleted = Expression.Call(typeof(EF), nameof(EF.Property), [typeof(bool)], parameter, Expression.Constant(nameof(ISoftDelete.IsDeleted)));
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(Expression.Not(isDeleted), parameter));
        }

        // Tables used by the MassTransit outbox (publish) and inbox (consume)
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyCrossCuttingRules();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyCrossCuttingRules();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>Fills the audit columns and turns a delete into a soft delete.</summary>
    private void ApplyCrossCuttingRules()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditable auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedOn = now;
                }

                if (entry.State is EntityState.Added or EntityState.Modified)
                {
                    auditable.ModifiedOn = now;
                }
            }

            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDelete soft)
            {
                entry.State = EntityState.Modified;
                soft.IsDeleted = true;
                soft.DeletedOn = now;
            }
        }
    }
}
