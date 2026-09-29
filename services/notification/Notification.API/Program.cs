using Notification.Api;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("notification");
builder.Services.AddDbContext<NotificationDb>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Db")));
builder.AddMessaging<NotificationDb>();

var app = builder.Build();

app.UseServiceDefaults();
app.EnsureDatabaseCreated<NotificationDb>();

app.Run();
