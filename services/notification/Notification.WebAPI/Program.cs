using Notification.Infrastructure;
using Notification.Persistence;
using ParkingSystem.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("notification");
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

app.UseServiceDefaults();
app.EnsureDatabaseCreated<ApplicationDbContext>();

app.Run();
