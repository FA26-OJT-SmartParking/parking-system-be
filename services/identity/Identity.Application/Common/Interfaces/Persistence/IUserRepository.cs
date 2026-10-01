using Identity.Domain.Entities;

namespace Identity.Application.Common.Interfaces.Persistence;

public interface IUserRepository : IRepository<User>
{
    /// <summary>The sample user under the given name.</summary>
    User GetSample(string name);
}
