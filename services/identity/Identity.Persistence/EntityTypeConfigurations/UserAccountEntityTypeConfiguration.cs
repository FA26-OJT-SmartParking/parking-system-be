using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Persistence.EntityTypeConfigurations;

public class UserAccountEntityTypeConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("user_accounts");
        builder.ConfigureBaseEntity();
        builder.Property(e => e.UserName).HasColumnName("user_name").HasMaxLength(255);
        builder.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
        builder.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.LastLogin).HasColumnName("last_login").HasColumnType("timestamptz");
        builder.Property(e => e.FailedLoginAttempts).HasColumnName("failed_login_attempts").HasDefaultValue(0);
        builder.Property(e => e.UserId).HasColumnName("user_id").HasColumnType("uuid");
        builder.HasIndex(e => e.UserName).IsUnique();

        builder.HasOne(e => e.User)
            .WithOne(u => u.UserAccount)
            .HasForeignKey<UserAccount>(e => e.UserId)
            .IsRequired(false);
    }
}
