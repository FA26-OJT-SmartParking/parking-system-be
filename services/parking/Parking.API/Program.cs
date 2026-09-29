using Parking.API.GrpcServices;
using Parking.API.Hubs;
using Parking.API.Workers;
using Parking.Infrastructure;
using Parking.Infrastructure.Persistence;
using ParkingSystem.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("parking");
builder.Services.AddParkingInfrastructure(builder.Configuration);
builder.AddMessaging<ParkingDb>();
builder.Services.AddGrpc();
builder.Services.AddSignalR();
builder.Services.AddHostedService<MqttSlotListener>();

var app = builder.Build();

app.UseServiceDefaults();
app.EnsureDatabaseCreated<ParkingDb>();
app.MapGrpcService<ParkingGrpcService>();
app.MapHub<ParkingHub>("/hubs/parking");

app.Run();
