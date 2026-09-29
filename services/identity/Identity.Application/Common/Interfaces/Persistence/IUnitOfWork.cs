namespace Identity.Application.Common.Interfaces.Persistence;

public interface IUnitOfWork
{
    IUserRepository UserRepository { get; }

    IUserAccountRepository UserAccountRepository { get; }

    IUserAccountSessionRepository UserAccountSessionRepository { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
