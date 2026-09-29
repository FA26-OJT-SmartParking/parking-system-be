using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Persistence.EntityTypeConfigurations;

public class UserEntityTypeConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.ConfigureBaseEntity();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(255);
        builder.Property(e => e.PhoneNumber).HasColumnName("phone_number").HasMaxLength(255);
        builder.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
    }
}
