using Identity.Application.Common.Interfaces.Persistence;
using Identity.Domain.Entities;
using Identity.Persistence.Repositories.BaseRepository;

namespace Identity.Persistence.Repositories;

public class UserRepository(ApplicationDbContext dbContext) : GenericRepository<User>(dbContext), IUserRepository
{
    private static readonly Guid SampleUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>Builds the user in memory and does not query the database. A real lookup would start from Query().</summary>
    public User GetSample(string name) => new()
    {
        Id = SampleUserId,
        Name = name,
        Email = "sample.user@example.com",
        PhoneNumber = "0900000000",
    };
}
