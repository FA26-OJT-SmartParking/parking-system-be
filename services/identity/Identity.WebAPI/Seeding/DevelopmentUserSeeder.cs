using Identity.Application.Common.Interfaces.Persistence;
using Identity.Application.Common.Interfaces.Services;
using Identity.Domain.Entities;
using Identity.Domain.Enum;

namespace Identity.WebAPI.Seeding;

/// <summary>
/// In Development, creates one active account from Seed:UserName and Seed:Password (set them in deploy/.env)
/// so that POST /api/auth/login can be tried. Does nothing when they are empty.
/// </summary>
internal static class DevelopmentUserSeeder
{
    public static async Task SeedAsync(WebApplication app)
    {
        var userName = app.Configuration["Seed:UserName"];
        var password = app.Configuration["Seed:Password"];
        if (!app.Environment.IsDevelopment() || string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        if (await unitOfWork.UserAccountRepository.GetByUserNameAsync(userName, CancellationToken.None) is not null)
        {
            return;
        }

        await unitOfWork.UserAccountRepository.AddAsync(new UserAccount
        {
            UserName = userName,
            PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Hash(password),
            Status = AccountStatus.Active,
            User = new User { Name = userName },
        }, CancellationToken.None);
        await unitOfWork.SaveChangesAsync();
        app.Logger.LogInformation("Created the development account {UserName}", userName);
    }
}
