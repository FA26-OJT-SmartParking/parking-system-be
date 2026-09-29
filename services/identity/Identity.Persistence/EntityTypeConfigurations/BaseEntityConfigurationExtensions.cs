using Identity.Domain.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Persistence.EntityTypeConfigurations;

internal static class BaseEntityConfigurationExtensions
{
    /// <summary>Maps the columns every BaseEntity has (snake_case names, PostgreSQL types).</summary>
    public static void ConfigureBaseEntity<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : BaseEntity
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.CreatedOn).HasColumnName("created_on").HasColumnType("timestamptz");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(255);
        builder.Property(e => e.ModifiedOn).HasColumnName("modified_on").HasColumnType("timestamptz");
        builder.Property(e => e.ModifiedBy).HasColumnName("modified_by").HasMaxLength(255);
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(e => e.DeletedOn).HasColumnName("deleted_on").HasColumnType("timestamptz");
    }
}
