namespace Payment.Domain.Base;

public interface ISoftDelete
{
    bool IsDeleted { get; set; }

    DateTimeOffset? DeletedOn { get; set; }
}
