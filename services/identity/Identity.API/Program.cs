using Identity.Infrastructure;
using Identity.Infrastructure.Persistence;
using ParkingSystem.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("identity");
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.AddMessaging<IdentityDb>();

var app = builder.Build();

app.UseServiceDefaults();
app.EnsureDatabaseCreated<IdentityDb>();

app.Run();
