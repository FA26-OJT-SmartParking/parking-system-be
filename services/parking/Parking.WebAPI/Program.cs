using Parking.WebAPI.Hubs;
using Parking.WebAPI.Workers;
using Parking.Infrastructure;
using Parking.Infrastructure.GRPC.Services;
using Parking.Persistence;
using ParkingSystem.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("parking");
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddSignalR();
builder.Services.AddHostedService<MqttSlotListener>();

var app = builder.Build();

app.UseServiceDefaults();
app.EnsureDatabaseCreated<ApplicationDbContext>();
app.MapGrpcService<ParkingGrpcService>();
app.MapHub<ParkingHub>("/hubs/parking");

app.Run();
