using Identity.Application.Common.Interfaces.Persistence;
using Identity.Domain.Entities;
using Identity.Persistence.Repositories.BaseRepository;

namespace Identity.Persistence.Repositories;

public class UserRepository(ApplicationDbContext dbContext) : GenericRepository<User>(dbContext), IUserRepository
{
}
