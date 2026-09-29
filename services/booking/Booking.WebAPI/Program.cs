using Booking.Application.Features.Availability;
using Booking.Infrastructure;
using Booking.Persistence;
using ParkingSystem.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("booking");
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.AddMessaging<ApplicationDbContext>();
builder.Services.AddScoped<GetLotAvailabilityHandler>();
builder.Services.AddControllers();

var app = builder.Build();

app.UseServiceDefaults();
app.EnsureDatabaseCreated<ApplicationDbContext>();
app.MapControllers();

app.Run();
