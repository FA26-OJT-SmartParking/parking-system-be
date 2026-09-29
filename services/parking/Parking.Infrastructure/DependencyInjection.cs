using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Parking.Application.Interfaces;
using Parking.Infrastructure.Persistence;

namespace Parking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddParkingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ParkingDb>(options => options.UseNpgsql(configuration.GetConnectionString("Db")));
        services.AddScoped<ISlotStateStore, SlotStateStore>();
        return services;
    }
}
