using Identity.Domain.Entities;

namespace Identity.Application.Common.Interfaces.Persistence;

public interface IUserRepository : IRepository<User>
{
    User GetSample(string name);
}
