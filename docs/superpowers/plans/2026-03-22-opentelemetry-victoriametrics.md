# OpenTelemetry Metrics → VictoriaMetrics Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expose a `/metrics` scrape endpoint (Prometheus format) so VictoriaMetrics can pull HTTP, runtime, EF Core, and custom business metrics from the TrainBooking API.

**Architecture:** OpenTelemetry SDK is registered in `Program.cs` with ASP.NET Core, runtime, and EF Core instrumentation; a Prometheus exporter maps `/metrics`. A new `BookingMetrics` singleton wraps a `Meter` and exposes typed counters; it is injected into `BookingService` and called after a booking is persisted. `BookingRequest` books exactly one seat, so `RecordSeatsBooked` always receives `1`.

**Tech Stack:** .NET 9, OpenTelemetry.Extensions.Hosting, OpenTelemetry.Instrumentation.AspNetCore, OpenTelemetry.Instrumentation.Runtime, OpenTelemetry.Exporter.Prometheus.AspNetCore, System.Diagnostics.Metrics, Microsoft.Extensions.Diagnostics.Testing (test only)

---

## File Map

| File | Action | Responsibility |
|---|---|---|
| `TrainBooking/TrainBooking.Api/TrainBooking.Api.csproj` | Modify | Add 4 OTel NuGet packages |
| `TrainBooking/TrainBooking.Tests/TrainBooking.Tests.csproj` | Modify | Add Microsoft.Extensions.Diagnostics.Testing |
| `TrainBooking/TrainBooking.Api/Program.cs` | Modify | Register OTel + map `/metrics` |
| `TrainBooking/TrainBooking.Api/Metrics/BookingMetrics.cs` | Create | Owns Meter and business counters |
| `TrainBooking/TrainBooking.Api/Services/BookingService.cs` | Modify | Inject BookingMetrics, call on booking creation |
| `TrainBooking/TrainBooking.Tests/BookingMetricsTests.cs` | Create | Unit tests for BookingMetrics counters |
| `TrainBooking/TrainBooking.Tests/MetricsEndpointTests.cs` | Create | Integration test for `/metrics` endpoint |
| `TrainBooking/TrainBooking.Tests/BookingServiceTests.cs` | Modify | Add BookingMetrics to all 6 constructor calls |

---

## Task 1: Add NuGet Packages

**Files:**
- Modify: `TrainBooking/TrainBooking.Api/TrainBooking.Api.csproj`
- Modify: `TrainBooking/TrainBooking.Tests/TrainBooking.Tests.csproj`

- [ ] **Step 1: Add API packages**

```bash
cd TrainBooking/TrainBooking.Api
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Runtime
dotnet add package OpenTelemetry.Exporter.Prometheus.AspNetCore
cd ../..
```

- [ ] **Step 2: Add test helper package**

```bash
cd TrainBooking/TrainBooking.Tests
dotnet add package Microsoft.Extensions.Diagnostics.Testing
cd ../..
```

- [ ] **Step 3: Verify restore**

```bash
cd TrainBooking && dotnet restore && cd ..
```

Expected: no errors.

- [ ] **Step 4: Commit**

```bash
git add TrainBooking/TrainBooking.Api/TrainBooking.Api.csproj \
        TrainBooking/TrainBooking.Tests/TrainBooking.Tests.csproj
git commit -m "chore: add OpenTelemetry and testing NuGet packages"
```

---

## Task 2: Create `BookingMetrics`

**Files:**
- Create: `TrainBooking/TrainBooking.Api/Metrics/BookingMetrics.cs`
- Create: `TrainBooking/TrainBooking.Tests/BookingMetricsTests.cs`

- [ ] **Step 1: Write the failing unit tests**

Create `TrainBooking/TrainBooking.Tests/BookingMetricsTests.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using TrainBooking.Api.Metrics;

namespace TrainBooking.Tests;

public class BookingMetricsTests
{
    private static (BookingMetrics metrics, MetricCollector<int> bookingsCollector, MetricCollector<int> seatsCollector) CreateSut()
    {
        var services = new ServiceCollection();
        services.AddMetrics();
        var provider = services.BuildServiceProvider();
        var meterFactory = provider.GetRequiredService<IMeterFactory>();

        // Collectors are created before BookingMetrics intentionally:
        // MetricCollector subscribes via IMeterFactory and will capture measurements
        // from any Meter named "TrainBooking" created by BookingMetrics below.
        var bookingsCollector = new MetricCollector<int>(meterFactory, "TrainBooking", "trainbooking.bookings.created");
        var seatsCollector = new MetricCollector<int>(meterFactory, "TrainBooking", "trainbooking.seats.booked");
        var metrics = new BookingMetrics(meterFactory);

        return (metrics, bookingsCollector, seatsCollector);
    }

    [Fact]
    public void RecordBookingCreated_IncrementsCounter()
    {
        var (metrics, bookingsCollector, _) = CreateSut();

        metrics.RecordBookingCreated("42");

        var measurement = Assert.Single(bookingsCollector.GetMeasurementSnapshot());
        Assert.Equal(1, measurement.Value);
        Assert.Equal("42", measurement.Tags["train.id"]);
    }

    [Fact]
    public void RecordSeatsBooked_IncrementsCounterByAmount()
    {
        var (metrics, _, seatsCollector) = CreateSut();

        metrics.RecordSeatsBooked(3, "42");

        var measurement = Assert.Single(seatsCollector.GetMeasurementSnapshot());
        Assert.Equal(3, measurement.Value);
        Assert.Equal("42", measurement.Tags["train.id"]);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
cd TrainBooking
dotnet test TrainBooking.Tests --filter "BookingMetricsTests" -v normal
```

Expected: compilation error — `BookingMetrics` does not exist yet.

- [ ] **Step 3: Create `BookingMetrics`**

Create `TrainBooking/TrainBooking.Api/Metrics/BookingMetrics.cs`:

```csharp
using System.Diagnostics.Metrics;

namespace TrainBooking.Api.Metrics;

public sealed class BookingMetrics
{
    private readonly Counter<int> _bookingsCreated;
    private readonly Counter<int> _seatsBooked;

    public BookingMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(new MeterOptions("TrainBooking"));
        _bookingsCreated = meter.CreateCounter<int>(
            "trainbooking.bookings.created",
            description: "Total number of bookings successfully created.");
        _seatsBooked = meter.CreateCounter<int>(
            "trainbooking.seats.booked",
            description: "Total number of seats booked.");
    }

    public void RecordBookingCreated(string trainId) =>
        _bookingsCreated.Add(1, new KeyValuePair<string, object?>("train.id", trainId));

    public void RecordSeatsBooked(int count, string trainId) =>
        _seatsBooked.Add(count, new KeyValuePair<string, object?>("train.id", trainId));
}
```

- [ ] **Step 4: Run the test to verify it passes**

```bash
dotnet test TrainBooking.Tests --filter "BookingMetricsTests" -v normal
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
cd ..
git add TrainBooking/TrainBooking.Api/Metrics/BookingMetrics.cs \
        TrainBooking/TrainBooking.Tests/BookingMetricsTests.cs
git commit -m "feat: add BookingMetrics with bookings and seats counters"
```

---

## Task 3: Wire OpenTelemetry in `Program.cs`

**Files:**
- Modify: `TrainBooking/TrainBooking.Api/Program.cs`
- Create: `TrainBooking/TrainBooking.Tests/MetricsEndpointTests.cs`

- [ ] **Step 1: Write the failing integration test**

Create `TrainBooking/TrainBooking.Tests/MetricsEndpointTests.cs`:

```csharp
using System.Net;
using TrainBooking.Tests.Infrastructure;

namespace TrainBooking.Tests;

public class MetricsEndpointTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GetMetrics_ReturnsOk()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/metrics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMetrics_ReturnsPrometheusContentType()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/metrics");
        var contentType = response.Content.Headers.ContentType?.MediaType;
        Assert.StartsWith("text/plain", contentType ?? string.Empty);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

```bash
cd TrainBooking
dotnet test TrainBooking.Tests --filter "MetricsEndpointTests" -v normal
```

Expected: FAIL — 404 Not Found.

- [ ] **Step 3: Register OTel in `Program.cs`**

The current `Program.cs` ends the service registrations with:

```csharp
builder.Services.AddScoped<IBookingService, BookingService>();
```

Add immediately after that line:

```csharp
builder.Services.AddSingleton<BookingMetrics>();

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("Microsoft.EntityFrameworkCore")
        .AddMeter("TrainBooking")
        .AddPrometheusExporter());
```

Further down, `Program.cs` currently has this section:

```csharp
app.UseHttpsRedirection();
app.MapControllers();

// Apply migrations on startup (relational DB only)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
        db.Database.Migrate();
}

app.Run();
```

Insert `app.MapPrometheusScrapingEndpoint();` immediately after `app.MapControllers()` so it reads:

```csharp
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
```

Add the following using at the top of `Program.cs`:

```csharp
using TrainBooking.Api.Metrics;
```

- [ ] **Step 4: Run the integration test to verify it passes**

```bash
dotnet test TrainBooking.Tests --filter "MetricsEndpointTests" -v normal
```

Expected: 2 tests PASS.

- [ ] **Step 5: Run the full test suite**

```bash
dotnet test TrainBooking.Tests -v normal
```

Expected: all tests PASS.

- [ ] **Step 6: Commit**

```bash
cd ..
git add TrainBooking/TrainBooking.Api/Program.cs \
        TrainBooking/TrainBooking.Tests/MetricsEndpointTests.cs
git commit -m "feat: register OpenTelemetry and expose /metrics scrape endpoint"
```

---

## Task 4: Inject `BookingMetrics` into `BookingService`

**Files:**
- Modify: `TrainBooking/TrainBooking.Tests/BookingServiceTests.cs` (tests first — TDD)
- Modify: `TrainBooking/TrainBooking.Api/Services/BookingService.cs`

- [ ] **Step 1: Update `BookingServiceTests.cs` — add helper and update all 6 constructor calls**

This is the TDD step: update the tests to pass a third argument to `BookingService`. They will fail to compile until `BookingService` is updated in Step 3.

Add these usings at the top of `BookingServiceTests.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using TrainBooking.Api.Metrics;
```

Add this private helper method inside `BookingServiceTests`:

```csharp
private static BookingMetrics CreateMetrics()
{
    var services = new ServiceCollection();
    services.AddMetrics();
    var provider = services.BuildServiceProvider();
    return new BookingMetrics(provider.GetRequiredService<IMeterFactory>());
}
```

Replace every occurrence of this constructor call (appears 6 times, at lines 24, 44, 66, 83, 97, 117):

```csharp
var service = new BookingService(ctx, Microsoft.Extensions.Logging.Abstractions.NullLogger<TrainBooking.Api.Services.BookingService>.Instance);
```

With:

```csharp
var service = new BookingService(ctx, Microsoft.Extensions.Logging.Abstractions.NullLogger<TrainBooking.Api.Services.BookingService>.Instance, CreateMetrics());
```

- [ ] **Step 2: Verify the tests fail to compile**

```bash
cd TrainBooking
dotnet test TrainBooking.Tests --filter "BookingServiceTests" -v normal
```

Expected: compilation error — `BookingService` constructor does not take 3 arguments yet.

- [ ] **Step 3: Update `BookingService.cs` to accept and use `BookingMetrics`**

In `BookingService.cs`, add this using at the top:

```csharp
using TrainBooking.Api.Metrics;
```

Add field after the existing `_logger` field:

```csharp
private readonly BookingMetrics _metrics;
```

Update the constructor signature:

```csharp
public BookingService(AppDbContext db, ILogger<BookingService> logger, BookingMetrics metrics)
{
    _db = db;
    _logger = logger;
    _metrics = metrics;
}
```

In `CreateBookingAsync`, insert two metric calls immediately after the existing success log line and before `return MapToResponse(...)`:

```csharp
// existing:
_logger.LogInformation("Booking {BookingReference} created for passenger {PassengerName}",
    reference, request.PassengerName);

// add:
_metrics.RecordBookingCreated(request.TrainId.ToString());
_metrics.RecordSeatsBooked(1, request.TrainId.ToString());

// existing:
return MapToResponse(booking, train.Name, seat);
```

- [ ] **Step 4: Run all tests**

```bash
dotnet test TrainBooking.Tests -v normal
```

Expected: all tests PASS.

- [ ] **Step 5: Commit**

```bash
cd ..
git add TrainBooking/TrainBooking.Api/Services/BookingService.cs \
        TrainBooking/TrainBooking.Tests/BookingServiceTests.cs
git commit -m "feat: record booking metrics on successful booking creation"
```

---

## Task 5: Smoke Test the `/metrics` Endpoint

- [ ] **Step 1: Run the API locally**

```bash
cd TrainBooking/TrainBooking.Api
dotnet run
```

- [ ] **Step 2: Curl the metrics endpoint**

```bash
curl http://localhost:5000/metrics | head -50
```

Expected: Prometheus text format output starting with lines like:

```
# HELP http_server_request_duration_seconds ...
# TYPE http_server_request_duration_seconds histogram
```

- [ ] **Step 3: Create a booking and verify custom counter appears**

```bash
curl -X POST http://localhost:5000/api/bookings \
  -H "Content-Type: application/json" \
  -d '{"trainId":1,"seatId":1,"passengerName":"Test","passengerEmail":"test@test.com"}'

curl http://localhost:5000/metrics | grep trainbooking
```

Expected output includes:

```
trainbooking_bookings_created_total{train_id="1"} 1
trainbooking_seats_booked_total{train_id="1"} 1
```

- [ ] **Step 4: Configure VictoriaMetrics scrape**

In your VictoriaMetrics scrape config (`prometheus.yml` or equivalent):

```yaml
scrape_configs:
  - job_name: trainbooking
    static_configs:
      - targets: ['<your-api-host>:5000']
```

Verify in VictoriaMetrics UI: query `trainbooking_bookings_created_total`.
