using Notification.Application.Common.Interfaces.Persistence;
using Notification.Domain.Base;
using Microsoft.EntityFrameworkCore;

namespace Notification.Persistence.Repositories.BaseRepository;

public class GenericRepository<TEntity>(ApplicationDbContext dbContext) : IRepository<TEntity>
    where TEntity : BaseEntity
{
    protected readonly ApplicationDbContext dbContext = dbContext;

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken) =>
        dbContext.Set<TEntity>().AddAsync(entity, cancellationToken).AsTask();

    public Task AddRangeAsync(List<TEntity> entities, CancellationToken cancellationToken) =>
        dbContext.Set<TEntity>().AddRangeAsync(entities, cancellationToken);

    public void Update(TEntity entity) => dbContext.Set<TEntity>().Update(entity);

    public void Remove(TEntity entity) => dbContext.Set<TEntity>().Remove(entity);

    public IQueryable<TEntity> Query() => dbContext.Set<TEntity>();

    public IQueryable<TEntity> QueryIncludingDeleted() => dbContext.Set<TEntity>().IgnoreQueryFilters();
}
