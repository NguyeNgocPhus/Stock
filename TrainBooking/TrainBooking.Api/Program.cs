using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using Serilog;
using TrainBooking.Api.Data;
using TrainBooking.Api.Logging;
using TrainBooking.Api.Metrics;
using TrainBooking.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, config) => config
    .Enrich.With<ActivityEnricher>()
    .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{TraceId}] {Message:lj}{NewLine}{Exception}"));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IBookingService, BookingService>();

builder.Services.AddSingleton<BookingMetrics>();

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("Microsoft.EntityFrameworkCore")
        .AddMeter("TrainBooking")
        .AddPrometheusExporter());

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapPrometheusScrapingEndpoint();

// Apply migrations on startup (relational DB only)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
        db.Database.Migrate();
}

app.Run();

public partial class Program { }
