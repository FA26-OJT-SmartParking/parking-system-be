using Booking.Api;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("booking");
builder.Services.AddDbContext<BookingDb>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Db")));
builder.AddMessaging<BookingDb>();

var app = builder.Build();

app.UseServiceDefaults();
app.EnsureDatabaseCreated<BookingDb>();

app.Run();
