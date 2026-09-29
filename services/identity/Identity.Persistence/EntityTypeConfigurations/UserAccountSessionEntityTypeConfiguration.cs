using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Persistence.EntityTypeConfigurations;

public class UserAccountSessionEntityTypeConfiguration : IEntityTypeConfiguration<UserAccountSession>
{
    public void Configure(EntityTypeBuilder<UserAccountSession> builder)
    {
        builder.ToTable("user_account_sessions");
        builder.ConfigureBaseEntity();
        builder.Property(e => e.AccessTokenHash).HasColumnName("access_token_hash").HasMaxLength(128);
        builder.Property(e => e.RefreshTokenHash).HasColumnName("refresh_token_hash").HasMaxLength(128);
        builder.Property(e => e.ExpiredOn).HasColumnName("expired_on").HasColumnType("timestamptz");
        builder.Property(e => e.UserAccountId).HasColumnName("user_account_id").HasColumnType("uuid");
        builder.HasIndex(e => e.RefreshTokenHash);

        builder.HasOne(e => e.UserAccount)
            .WithMany(a => a.UserAccountSessions)
            .HasForeignKey(e => e.UserAccountId)
            .IsRequired(false);
    }
}
