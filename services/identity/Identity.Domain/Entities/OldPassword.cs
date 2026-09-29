using Identity.Domain.Base;

namespace Identity.Domain.Entities;

public class OldPassword : BaseEntity
{
    public string? OldPasswordHash { get; set; }

    public Guid? UserAccountId { get; set; }

    public UserAccount? UserAccount { get; set; }
}
