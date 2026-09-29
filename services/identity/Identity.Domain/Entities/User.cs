using Identity.Domain.Base;

namespace Identity.Domain.Entities;

public class User : BaseEntity
{
    public string? Name { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public UserAccount? UserAccount { get; set; }
}
