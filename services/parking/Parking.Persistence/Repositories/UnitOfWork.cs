using Parking.Application.Common.Interfaces.Persistence;

namespace Parking.Persistence.Repositories;

public class UnitOfWork(ApplicationDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);
}
