using Payment.Application;
using Payment.Infrastructure.GRPC.Services;
using Payment.Infrastructure;
using Payment.Persistence;
using Payment.WebAPI.Middleware;
using Microsoft.AspNetCore.Mvc;
using ParkingSystem.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Serilog, OpenTelemetry, health checks and JWT validation
builder.AddServiceDefaults("payment");
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = ErrorExceptionHandler.InvalidModelState);

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<ExceptionHandlingMiddleware>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
    });
}

// Request logging, then the error middleware, then authentication and authorization, then /health
app.UseServiceDefaults(application => application.UseMiddleware<ExceptionHandlingMiddleware>());
app.EnsureDatabaseCreated<ApplicationDbContext>();
app.MapGrpcService<PaymentGrpcService>();
app.MapControllers();

app.Run();
