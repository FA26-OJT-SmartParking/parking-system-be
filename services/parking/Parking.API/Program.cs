using Parking.Api;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("parking");
builder.Services.AddDbContext<ParkingDb>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Db")));
builder.AddMessaging<ParkingDb>();
builder.Services.AddSignalR();
builder.Services.AddHostedService<MqttSlotListener>();

var app = builder.Build();

app.UseServiceDefaults();
app.EnsureDatabaseCreated<ParkingDb>();
app.MapHub<ParkingHub>("/hubs/parking");

app.Run();
