using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Payment.Api;

public class PaymentDb(DbContextOptions<PaymentDb> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tables used by the MassTransit outbox (publish) and inbox (consume)
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
