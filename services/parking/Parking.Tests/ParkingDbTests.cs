using Parking.Api;
using Microsoft.EntityFrameworkCore;

namespace Parking.Tests;

public class ParkingDbTests
{
    [Fact]
    public void Model_WhenBuilt_ContainsMassTransitOutboxTables()
    {
        // Building the EF Core model does not open a database connection
        var options = new DbContextOptionsBuilder<ParkingDb>().UseNpgsql("Host=localhost").Options;
        using var db = new ParkingDb(options);

        var tables = db.Model.GetEntityTypes().Select(entity => entity.GetTableName()).ToList();

        Assert.Contains("InboxState", tables);
        Assert.Contains("OutboxMessage", tables);
        Assert.Contains("OutboxState", tables);
    }
}
