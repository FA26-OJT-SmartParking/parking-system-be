using Payment.Infrastructure;
using Payment.Infrastructure.GRPC.Services;
using Payment.Persistence;
using ParkingSystem.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("payment");
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

app.UseServiceDefaults();
app.EnsureDatabaseCreated<ApplicationDbContext>();
app.MapGrpcService<PaymentGrpcService>();

app.Run();
