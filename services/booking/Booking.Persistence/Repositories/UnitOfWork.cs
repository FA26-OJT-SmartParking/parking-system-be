using Booking.Application.Common.Interfaces.Persistence;

namespace Booking.Persistence.Repositories;

public class UnitOfWork(ApplicationDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);
}
