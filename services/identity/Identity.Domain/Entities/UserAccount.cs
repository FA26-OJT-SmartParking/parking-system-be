using Identity.Domain.Base;
using Identity.Domain.Enum;

namespace Identity.Domain.Entities;

public class UserAccount : BaseEntity
{
    public string? UserName { get; set; }

    public string? PasswordHash { get; set; }

    public AccountStatus Status { get; set; }

    public DateTimeOffset? LastLogin { get; set; }

    public int FailedLoginAttempts { get; set; }

    public Guid? UserId { get; set; }

    public User? User { get; set; }

    public List<UserAccountSession>? UserAccountSessions { get; set; }

    public List<OldPassword>? OldPasswords { get; set; }
}
