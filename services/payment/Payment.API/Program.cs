using Payment.API.GrpcServices;
using Payment.Infrastructure;
using Payment.Infrastructure.Persistence;
using ParkingSystem.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("payment");
builder.Services.AddPaymentInfrastructure(builder.Configuration);
builder.AddMessaging<PaymentDb>();
builder.Services.AddGrpc();

var app = builder.Build();

app.UseServiceDefaults();
app.EnsureDatabaseCreated<PaymentDb>();
app.MapGrpcService<PaymentGrpcService>();

app.Run();
