using Identity.Api;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("identity");
builder.Services.AddDbContext<IdentityDb>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Db")));
builder.AddMessaging<IdentityDb>();

var app = builder.Build();

app.UseServiceDefaults();
app.EnsureDatabaseCreated<IdentityDb>();

app.Run();
