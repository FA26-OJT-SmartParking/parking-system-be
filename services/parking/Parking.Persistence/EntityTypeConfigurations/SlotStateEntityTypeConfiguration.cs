using Parking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Parking.Persistence.EntityTypeConfigurations;

public class SlotStateEntityTypeConfiguration : IEntityTypeConfiguration<SlotState>
{
    public void Configure(EntityTypeBuilder<SlotState> builder)
    {
        builder.HasKey(s => new { s.LotId, s.Code });
        builder.Property(s => s.Code).HasMaxLength(32);
        builder.Property(s => s.Status).HasMaxLength(32);
    }
}
