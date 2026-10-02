using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ParkingSystem.ServiceDefaults;

public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Applies the EF Core migrations of <typeparamref name="TDbContext"/> on startup in Development.
    /// Elsewhere, run them as a deployment step (dotnet ef database update) before the new version starts.
    /// </summary>
    public static WebApplication MigrateDatabase<TDbContext>(this WebApplication app)
        where TDbContext : DbContext
    {
        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<TDbContext>().Database.Migrate();
        }
        return app;
    }
}
