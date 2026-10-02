using Identity.Application.Common.Interfaces.Persistence;
using Identity.Domain.Entities;
using Identity.Persistence.Repositories.BaseRepository;
using Microsoft.EntityFrameworkCore;

namespace Identity.Persistence.Repositories;

public class UserAccountRepository(ApplicationDbContext dbContext) : GenericRepository<UserAccount>(dbContext), IUserAccountRepository
{
    public Task<UserAccount?> GetByUserNameAsync(string userName, CancellationToken cancellationToken) =>
        Query().FirstOrDefaultAsync(account => account.UserName == userName, cancellationToken);
}
