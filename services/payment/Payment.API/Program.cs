using Payment.Infrastructure;
using Payment.Infrastructure.Persistence;
using ParkingSystem.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("payment");
builder.Services.AddPaymentInfrastructure(builder.Configuration);
builder.AddMessaging<PaymentDb>();

var app = builder.Build();

app.UseServiceDefaults();
app.EnsureDatabaseCreated<PaymentDb>();

app.Run();
