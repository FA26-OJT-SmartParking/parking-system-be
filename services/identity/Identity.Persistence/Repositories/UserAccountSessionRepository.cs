using Identity.Application.Common.Interfaces.Persistence;
using Identity.Domain.Entities;
using Identity.Persistence.Repositories.BaseRepository;

namespace Identity.Persistence.Repositories;

public class UserAccountSessionRepository(ApplicationDbContext dbContext) : GenericRepository<UserAccountSession>(dbContext), IUserAccountSessionRepository
{
}
