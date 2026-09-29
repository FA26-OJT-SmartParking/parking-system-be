using Notification.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Notification.Tests;

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

    [Fact]
    public void Migrations_WhenModelChanged_AreUpToDate()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql("Host=localhost").Options;
        using var db = new ApplicationDbContext(options);

        // Fails when someone changes an entity or its mapping without adding a migration (dotnet ef migrations add)
        Assert.NotEmpty(db.Database.GetMigrations());
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public void Model_UsesTheSchemaOfTheService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql("Host=localhost").Options;
        using var db = new ApplicationDbContext(options);

        Assert.Equal("notification", db.Model.GetDefaultSchema());
        Assert.All(db.Model.GetEntityTypes(), entity => Assert.Equal("notification", entity.GetSchema()));
    }
}
