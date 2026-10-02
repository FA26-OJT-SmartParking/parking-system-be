using Identity.Application.Common.Interfaces.Persistence;
using Identity.Domain.Entities;

namespace Identity.Tests;

internal class InMemoryRepository<TEntity> : IRepository<TEntity>
    where TEntity : class
{
    public List<TEntity> Items { get; } = [];

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken)
    {
        Items.Add(entity);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(List<TEntity> entities, CancellationToken cancellationToken)
    {
        Items.AddRange(entities);
        return Task.CompletedTask;
    }

    public void Update(TEntity entity)
    {
    }

    public void Remove(TEntity entity) => Items.Remove(entity);

    public IQueryable<TEntity> Query() => Items.AsQueryable();

    public IQueryable<TEntity> QueryIncludingDeleted() => Items.AsQueryable();
}

internal sealed class FakeUserRepository : InMemoryRepository<User>, IUserRepository
{
}

internal sealed class FakeUserAccountRepository : InMemoryRepository<UserAccount>, IUserAccountRepository
{
    public Task<UserAccount?> GetByUserNameAsync(string userName, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(account => account.UserName == userName));
}

internal sealed class FakeUserAccountSessionRepository : InMemoryRepository<UserAccountSession>, IUserAccountSessionRepository
{
}

/// <summary>Repositories in memory, so the tests need no database.</summary>
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    private readonly FakeUserRepository users = new();
    private readonly FakeUserAccountRepository accounts = new();
    private readonly FakeUserAccountSessionRepository sessions = new();

    public int Saves { get; private set; }

    public FakeUserAccountRepository Accounts => accounts;

    public FakeUserAccountSessionRepository Sessions => sessions;

    public IUserRepository UserRepository => users;

    public IUserAccountRepository UserAccountRepository => accounts;

    public IUserAccountSessionRepository UserAccountSessionRepository => sessions;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Saves++;
        return Task.FromResult(1);
    }
}
