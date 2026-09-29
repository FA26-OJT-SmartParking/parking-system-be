using Booking.Application.Features.Availability;
using Booking.Infrastructure;
using Booking.Infrastructure.Persistence;
using ParkingSystem.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("booking");
builder.Services.AddBookingInfrastructure(builder.Configuration);
builder.AddMessaging<BookingDb>();
builder.Services.AddScoped<GetLotAvailabilityHandler>();
builder.Services.AddControllers();

var app = builder.Build();

app.UseServiceDefaults();
app.EnsureDatabaseCreated<BookingDb>();
app.MapControllers();

app.Run();
