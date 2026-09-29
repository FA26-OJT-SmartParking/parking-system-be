using Notification.Infrastructure;
using Notification.Infrastructure.Persistence;
using ParkingSystem.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("notification");
builder.Services.AddNotificationInfrastructure(builder.Configuration);
builder.AddMessaging<NotificationDb>();

var app = builder.Build();

app.UseServiceDefaults();
app.EnsureDatabaseCreated<NotificationDb>();

app.Run();
