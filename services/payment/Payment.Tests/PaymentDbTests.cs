using Payment.Api;
using Microsoft.EntityFrameworkCore;

namespace Payment.Tests;

public class PaymentDbTests
{
    [Fact]
    public void Model_WhenBuilt_ContainsMassTransitOutboxTables()
    {
        // Building the EF Core model does not open a database connection
        var options = new DbContextOptionsBuilder<PaymentDb>().UseNpgsql("Host=localhost").Options;
        using var db = new PaymentDb(options);

        var tables = db.Model.GetEntityTypes().Select(entity => entity.GetTableName()).ToList();

        Assert.Contains("InboxState", tables);
        Assert.Contains("OutboxMessage", tables);
        Assert.Contains("OutboxState", tables);
    }
}
