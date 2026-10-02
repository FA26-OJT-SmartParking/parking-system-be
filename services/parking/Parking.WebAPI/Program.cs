using Parking.Application;
using Parking.Application.Common.Interfaces.Services;
using Parking.Infrastructure.GRPC.Services;
using Parking.WebAPI.Hubs;
using Parking.Infrastructure;
using Parking.Persistence;
using Parking.WebAPI.Middleware;
using Microsoft.AspNetCore.Mvc;
using ParkingSystem.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Serilog, OpenTelemetry, health checks and JWT validation
builder.AddServiceDefaults("parking");
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = ErrorExceptionHandler.InvalidModelState);

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddSignalR();
builder.Services.AddSingleton<ISlotStatusNotifier, SlotStatusNotifier>();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<ExceptionHandlingMiddleware>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
    });
}

// Request logging, then the error middleware, then authentication and authorization, then /health
app.UseServiceDefaults(application => application.UseMiddleware<ExceptionHandlingMiddleware>());
app.UseStatusCodePages(ErrorExceptionHandler.WriteStatusCodeBody);
app.MigrateDatabase<ApplicationDbContext>();
// Called by the other services inside the Docker network only; the gateway does not route to it
app.MapGrpcService<ParkingGrpcService>().AllowAnonymous();
app.MapHub<ParkingHub>("/hubs/parking").AllowAnonymous();
app.MapControllers();

app.Run();
