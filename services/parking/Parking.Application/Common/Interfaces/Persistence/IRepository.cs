namespace Parking.Application.Common.Interfaces.Persistence;

public interface IRepository<TEntity>
    where TEntity : class
{
    Task AddAsync(TEntity entity, CancellationToken cancellationToken);

    Task AddRangeAsync(List<TEntity> entities, CancellationToken cancellationToken);

    void Update(TEntity entity);

    void Remove(TEntity entity);

    IQueryable<TEntity> Query();

    IQueryable<TEntity> QueryIncludingDeleted();
}
