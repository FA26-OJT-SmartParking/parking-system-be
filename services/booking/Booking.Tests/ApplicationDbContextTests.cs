using Booking.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Booking.Tests;

public class ApplicationDbContextTests
{
    [Fact]
    public void Model_WhenBuilt_ContainsMassTransitOutboxTables()
    {
        // Building the EF Core model does not open a database connection
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql("Host=localhost").Options;
        using var db = new ApplicationDbContext(options);

        var tables = db.Model.GetEntityTypes().Select(entity => entity.GetTableName()).ToList();

        Assert.Contains("InboxState", tables);
        Assert.Contains("OutboxMessage", tables);
        Assert.Contains("OutboxState", tables);
    }
}
