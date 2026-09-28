using Identity.Api;
using Microsoft.EntityFrameworkCore;

namespace Identity.Tests;

public class IdentityDbTests
{
    [Fact]
    public void Model_WhenBuilt_ContainsMassTransitOutboxTables()
    {
        // Building the EF Core model does not open a database connection
        var options = new DbContextOptionsBuilder<IdentityDb>().UseNpgsql("Host=localhost").Options;
        using var db = new IdentityDb(options);

        var tables = db.Model.GetEntityTypes().Select(entity => entity.GetTableName()).ToList();

        Assert.Contains("InboxState", tables);
        Assert.Contains("OutboxMessage", tables);
        Assert.Contains("OutboxState", tables);
    }
}
