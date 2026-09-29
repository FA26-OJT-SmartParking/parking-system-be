using Payment.WebAPI.GrpcServices;
using Payment.Infrastructure;
using Payment.Persistence;
using ParkingSystem.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("payment");
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.AddMessaging<ApplicationDbContext>();
builder.Services.AddGrpc();

var app = builder.Build();

app.UseServiceDefaults();
app.EnsureDatabaseCreated<ApplicationDbContext>();
app.MapGrpcService<PaymentGrpcService>();

app.Run();
