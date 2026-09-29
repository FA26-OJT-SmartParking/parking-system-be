namespace Identity.Domain.Base;

/// <summary>Common columns of every table: identity, audit and soft delete.</summary>
public class BaseEntity : IAuditable, ISoftDelete
{
    public Guid Id { get; set; }

    public DateTimeOffset CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset ModifiedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedOn { get; set; }
}
