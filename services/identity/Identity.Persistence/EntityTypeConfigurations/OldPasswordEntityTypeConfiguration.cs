using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Persistence.EntityTypeConfigurations;

public class OldPasswordEntityTypeConfiguration : IEntityTypeConfiguration<OldPassword>
{
    public void Configure(EntityTypeBuilder<OldPassword> builder)
    {
        builder.ToTable("old_passwords");
        builder.ConfigureBaseEntity();
        builder.Property(e => e.OldPasswordHash).HasColumnName("old_password_hash").HasMaxLength(255);
        builder.Property(e => e.UserAccountId).HasColumnName("user_account_id").HasColumnType("uuid");

        builder.HasOne(e => e.UserAccount)
            .WithMany(a => a.OldPasswords)
            .HasForeignKey(e => e.UserAccountId)
            .IsRequired(false);
    }
}
