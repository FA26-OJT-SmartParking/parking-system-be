using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Notification.Persistence;

/// <summary>
/// Used only by the `dotnet ef` tools (migrations). No connection is opened; the real connection string
/// comes from configuration when the service runs.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql("Host=localhost;Database=design_time").Options);
}
