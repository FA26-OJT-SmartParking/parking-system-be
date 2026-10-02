using Identity.Domain.Entities;

namespace Identity.Application.Common.Interfaces.Persistence;

public interface IUserAccountRepository : IRepository<UserAccount>
{
    /// <summary>The account with this user name, or null. Deleted accounts are not returned.</summary>
    Task<UserAccount?> GetByUserNameAsync(string userName, CancellationToken cancellationToken);
}
