using Payment.Api;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("payment");
builder.Services.AddDbContext<PaymentDb>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Db")));
builder.AddMessaging<PaymentDb>();

var app = builder.Build();

app.UseServiceDefaults();
app.EnsureDatabaseCreated<PaymentDb>();

app.Run();
