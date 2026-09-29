using Identity.Application.Common.Interfaces.Persistence;

namespace Identity.Persistence.Repositories;

public class UnitOfWork(ApplicationDbContext dbContext) : IUnitOfWork
{
    private IUserRepository? userRepository;
    private IUserAccountRepository? userAccountRepository;
    private IUserAccountSessionRepository? userAccountSessionRepository;

    public IUserRepository UserRepository => userRepository ??= new UserRepository(dbContext);

    public IUserAccountRepository UserAccountRepository => userAccountRepository ??= new UserAccountRepository(dbContext);

    public IUserAccountSessionRepository UserAccountSessionRepository => userAccountSessionRepository ??= new UserAccountSessionRepository(dbContext);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);
}
