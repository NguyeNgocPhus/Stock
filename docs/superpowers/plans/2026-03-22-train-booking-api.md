# Train Booking API Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a .NET 9 REST API that allows passengers to list trains, view available seats, create bookings, and retrieve bookings by reference — backed by PostgreSQL.

**Architecture:** Controller-based ASP.NET Core Web API with a service layer (`BookingService`) handling all business logic. EF Core 9 with Npgsql manages persistence. Data is seeded via `HasData()` — no admin endpoints needed. Integration tests use an InMemory database override via `CustomWebApplicationFactory` so no live PostgreSQL is required to run the test suite.

**Tech Stack:** .NET 9, ASP.NET Core Web API, Entity Framework Core 9, Npgsql (PostgreSQL), xUnit + EF Core InMemory (tests), Swagger/OpenAPI

---

## File Map

| File | Purpose |
|------|---------|
| `TrainBooking.Api/Models/Train.cs` | Train entity |
| `TrainBooking.Api/Models/Seat.cs` | Seat entity |
| `TrainBooking.Api/Models/Booking.cs` | Booking entity |
| `TrainBooking.Api/DTOs/BookingRequest.cs` | POST /api/bookings request body |
| `TrainBooking.Api/DTOs/BookingResponse.cs` | Booking response body (POST 201 + GET) |
| `TrainBooking.Api/DTOs/TrainDto.cs` | Train response body |
| `TrainBooking.Api/DTOs/SeatDto.cs` | Seat response body |
| `TrainBooking.Api/Data/AppDbContext.cs` | EF Core DbContext + seed data |
| `TrainBooking.Api/Services/IBookingService.cs` | Interface for BookingService |
| `TrainBooking.Api/Services/BookingService.cs` | All booking business logic |
| `TrainBooking.Api/Controllers/TrainsController.cs` | GET /api/trains, GET /api/trains/{id}, GET /api/trains/{id}/seats |
| `TrainBooking.Api/Controllers/BookingsController.cs` | POST /api/bookings, GET /api/bookings/{reference} |
| `TrainBooking.Api/Program.cs` | App bootstrap, DI registration, middleware |
| `TrainBooking.Tests/Infrastructure/CustomWebApplicationFactory.cs` | Test factory overriding DB with InMemory |
| `TrainBooking.Tests/AppDbContextTests.cs` | Seed data tests |
| `TrainBooking.Tests/BookingServiceTests.cs` | Unit tests for BookingService |
| `TrainBooking.Tests/TrainsControllerTests.cs` | Integration tests for TrainsController |
| `TrainBooking.Tests/BookingsControllerTests.cs` | Integration tests for BookingsController |

> Note: `TrainDto`, `SeatDto`, and `IBookingService` are plan additions not listed in the spec's project structure, but are standard practice for a clean controller-based API.

---

## Task 1: Scaffold Solution & Projects

**Files:**
- Create: `TrainBooking/TrainBooking.Api/TrainBooking.Api.csproj`
- Create: `TrainBooking/TrainBooking.Tests/TrainBooking.Tests.csproj`
- Create: `TrainBooking/TrainBooking.sln`

- [ ] **Step 1: Create the solution**

```bash
cd /Users/phunn/Documents/Code/Stock
dotnet new sln -n TrainBooking -o TrainBooking
```

- [ ] **Step 2: Create the API project**

```bash
cd /Users/phunn/Documents/Code/Stock/TrainBooking
dotnet new webapi -n TrainBooking.Api --framework net9.0
```

- [ ] **Step 3: Create the test project**

```bash
cd /Users/phunn/Documents/Code/Stock/TrainBooking
dotnet new xunit -n TrainBooking.Tests --framework net9.0
```

- [ ] **Step 4: Add projects to solution**

```bash
cd /Users/phunn/Documents/Code/Stock/TrainBooking
dotnet sln add TrainBooking.Api/TrainBooking.Api.csproj
dotnet sln add TrainBooking.Tests/TrainBooking.Tests.csproj
```

- [ ] **Step 5: Add project reference from Tests to Api**

```bash
cd /Users/phunn/Documents/Code/Stock/TrainBooking/TrainBooking.Tests
dotnet add reference ../TrainBooking.Api/TrainBooking.Api.csproj
```

- [ ] **Step 6: Add NuGet packages to API project**

```bash
cd /Users/phunn/Documents/Code/Stock/TrainBooking/TrainBooking.Api
dotnet add package Microsoft.EntityFrameworkCore --version 9.0.0
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 9.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 9.0.0
```

- [ ] **Step 7: Add NuGet packages to test project**

```bash
cd /Users/phunn/Documents/Code/Stock/TrainBooking/TrainBooking.Tests
dotnet add package Microsoft.EntityFrameworkCore.InMemory --version 9.0.0
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 9.0.0
```

- [ ] **Step 8: Verify build**

```bash
cd /Users/phunn/Documents/Code/Stock/TrainBooking
dotnet build
```
Expected: `Build succeeded.`

- [ ] **Step 9: Commit**

```bash
cd /Users/phunn/Documents/Code/Stock
git init
git add .
git commit -m "chore: scaffold solution with Api and Tests projects"
```

---

## Task 2: Domain Models

**Files:**
- Create: `TrainBooking.Api/Models/Train.cs`
- Create: `TrainBooking.Api/Models/Seat.cs`
- Create: `TrainBooking.Api/Models/Booking.cs`

- [ ] **Step 1: Create Train model**

`TrainBooking.Api/Models/Train.cs`:
```csharp
namespace TrainBooking.Api.Models;

public class Train
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }

    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
```

- [ ] **Step 2: Create Seat model**

`TrainBooking.Api/Models/Seat.cs`:
```csharp
namespace TrainBooking.Api.Models;

public class Seat
{
    public int Id { get; set; }
    public int TrainId { get; set; }
    public string Coach { get; set; } = string.Empty;
    public int Row { get; set; }
    public string Number { get; set; } = string.Empty;
    public bool IsBooked { get; set; } = false;

    public Train Train { get; set; } = null!;
}
```

- [ ] **Step 3: Create Booking model**

`TrainBooking.Api/Models/Booking.cs`:
```csharp
namespace TrainBooking.Api.Models;

public class Booking
{
    public int Id { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public int TrainId { get; set; }
    public int SeatId { get; set; }
    public string PassengerName { get; set; } = string.Empty;
    public string PassengerEmail { get; set; } = string.Empty;
    public DateTime BookedAt { get; set; }

    public Train Train { get; set; } = null!;
    public Seat Seat { get; set; } = null!;
}
```

- [ ] **Step 4: Verify build**

```bash
cd /Users/phunn/Documents/Code/Stock/TrainBooking
dotnet build
```
Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add TrainBooking.Api/Models/
git commit -m "feat: add Train, Seat, Booking domain models"
```

---

## Task 3: DTOs

**Files:**
- Create: `TrainBooking.Api/DTOs/TrainDto.cs`
- Create: `TrainBooking.Api/DTOs/SeatDto.cs`
- Create: `TrainBooking.Api/DTOs/BookingRequest.cs`
- Create: `TrainBooking.Api/DTOs/BookingResponse.cs`

- [ ] **Step 1: Create TrainDto**

`TrainBooking.Api/DTOs/TrainDto.cs`:
```csharp
namespace TrainBooking.Api.DTOs;

public class TrainDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
}
```

- [ ] **Step 2: Create SeatDto**

`TrainBooking.Api/DTOs/SeatDto.cs`:
```csharp
namespace TrainBooking.Api.DTOs;

public class SeatDto
{
    public int Id { get; set; }
    public string Coach { get; set; } = string.Empty;
    public int Row { get; set; }
    public string Number { get; set; } = string.Empty;
}
```

- [ ] **Step 3: Create BookingRequest**

`TrainBooking.Api/DTOs/BookingRequest.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace TrainBooking.Api.DTOs;

public class BookingRequest
{
    [Required]
    public int TrainId { get; set; }

    [Required]
    public int SeatId { get; set; }

    [Required]
    public string PassengerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string PassengerEmail { get; set; } = string.Empty;
}
```

- [ ] **Step 4: Create BookingResponse**

`TrainBooking.Api/DTOs/BookingResponse.cs`:
```csharp
namespace TrainBooking.Api.DTOs;

public class BookingResponse
{
    public string BookingReference { get; set; } = string.Empty;
    public string TrainName { get; set; } = string.Empty;
    public string Seat { get; set; } = string.Empty;
    public string PassengerName { get; set; } = string.Empty;
    public string PassengerEmail { get; set; } = string.Empty;
    public DateTime BookedAt { get; set; }
}
```

- [ ] **Step 5: Verify build**

```bash
cd /Users/phunn/Documents/Code/Stock/TrainBooking
dotnet build
```
Expected: `Build succeeded.`

- [ ] **Step 6: Commit**

```bash
git add TrainBooking.Api/DTOs/
git commit -m "feat: add DTOs for train, seat, and booking"
```

---

## Task 4: AppDbContext with Seed Data

**Files:**
- Create: `TrainBooking.Api/Data/AppDbContext.cs`
- Create: `TrainBooking.Tests/AppDbContextTests.cs`

> Spec: each train has coaches A–B, rows 1–10 = **20 seats per train**.

- [ ] **Step 1: Write failing test for seed data**

`TrainBooking.Tests/AppDbContextTests.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using TrainBooking.Api.Data;

namespace TrainBooking.Tests;

public class AppDbContextTests
{
    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void SeedData_ShouldHaveTwoTrains()
    {
        using var ctx = CreateInMemoryContext();
        ctx.Database.EnsureCreated();
        Assert.Equal(2, ctx.Trains.Count());
    }

    [Fact]
    public void SeedData_EachTrainShouldHave20Seats()
    {
        using var ctx = CreateInMemoryContext();
        ctx.Database.EnsureCreated();
        var train = ctx.Trains.First();
        var seats = ctx.Seats.Where(s => s.TrainId == train.Id).ToList();
        Assert.Equal(20, seats.Count); // 2 coaches × 10 rows
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
cd /Users/phunn/Documents/Code/Stock/TrainBooking
dotnet test --filter "AppDbContextTests" -v
```
Expected: FAIL — `AppDbContext` does not exist yet.

- [ ] **Step 3: Create AppDbContext**

`TrainBooking.Api/Data/AppDbContext.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using TrainBooking.Api.Models;

namespace TrainBooking.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Train> Trains => Set<Train>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.BookingReference)
            .IsUnique();

        modelBuilder.Entity<Seat>()
            .HasIndex(s => new { s.TrainId, s.Number })
            .IsUnique();

        // Seed trains
        modelBuilder.Entity<Train>().HasData(
            new Train { Id = 1, Name = "Express 101", Origin = "Hanoi", Destination = "Ho Chi Minh City",
                DepartureTime = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc),
                ArrivalTime = new DateTime(2026, 4, 1, 20, 0, 0, DateTimeKind.Utc) },
            new Train { Id = 2, Name = "Express 202", Origin = "Ho Chi Minh City", Destination = "Da Nang",
                DepartureTime = new DateTime(2026, 4, 2, 7, 0, 0, DateTimeKind.Utc),
                ArrivalTime = new DateTime(2026, 4, 2, 15, 0, 0, DateTimeKind.Utc) }
        );

        // Seed seats: 2 trains × 2 coaches × 10 rows = 40 seats total
        var seats = new List<Seat>();
        int seatId = 1;
        foreach (var trainId in new[] { 1, 2 })
        {
            foreach (var coach in new[] { "A", "B" })
            {
                for (int row = 1; row <= 10; row++)
                {
                    seats.Add(new Seat
                    {
                        Id = seatId++,
                        TrainId = trainId,
                        Coach = coach,
                        Row = row,
                        Number = $"{row}{coach}",
                        IsBooked = false
                    });
                }
            }
        }
        modelBuilder.Entity<Seat>().HasData(seats);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
cd /Users/phunn/Documents/Code/Stock/TrainBooking
dotnet test --filter "AppDbContextTests" -v
```
Expected: Both tests PASS.

- [ ] **Step 5: Commit**

```bash
git add TrainBooking.Api/Data/ TrainBooking.Tests/AppDbContextTests.cs
git commit -m "feat: add AppDbContext with seed data (2 trains, 20 seats each)"
```

---

## Task 5: CustomWebApplicationFactory for Integration Tests

**Files:**
- Create: `TrainBooking.Tests/Infrastructure/CustomWebApplicationFactory.cs`

Integration tests must not require a live PostgreSQL instance. This factory overrides the DB registration with an InMemory provider.

- [ ] **Step 1: Create the factory**

`TrainBooking.Tests/Infrastructure/CustomWebApplicationFactory.cs`:
```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrainBooking.Api.Data;

namespace TrainBooking.Tests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real PostgreSQL registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Add InMemory database
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));
        });

        builder.UseEnvironment("Development");
    }
}
```

- [ ] **Step 2: Verify build**

```bash
cd /Users/phunn/Documents/Code/Stock/TrainBooking
dotnet build
```
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add TrainBooking.Tests/Infrastructure/
git commit -m "test: add CustomWebApplicationFactory with InMemory DB override"
```

---

## Task 6: BookingService

**Files:**
- Create: `TrainBooking.Api/Services/IBookingService.cs`
- Create: `TrainBooking.Api/Services/BookingService.cs`
- Create: `TrainBooking.Tests/BookingServiceTests.cs`

> The service checks `seat.IsBooked` from the already-loaded EF entity. No raw SQL `FOR UPDATE` is used, keeping the service testable with InMemory. In production against PostgreSQL, the `IsolationLevel.Serializable` transaction prevents concurrent double-bookings at the DB level.

- [ ] **Step 1: Write failing tests**

`TrainBooking.Tests/BookingServiceTests.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using TrainBooking.Api.Data;
using TrainBooking.Api.DTOs;
using TrainBooking.Api.Services;

namespace TrainBooking.Tests;

public class BookingServiceTests
{
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new AppDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public async Task CreateBooking_ValidRequest_ReturnsBookingResponse()
    {
        using var ctx = CreateContext();
        var service = new BookingService(ctx);
        var train = ctx.Trains.First();
        var seat = ctx.Seats.First(s => s.TrainId == train.Id);

        var result = await service.CreateBookingAsync(new BookingRequest
        {
            TrainId = train.Id, SeatId = seat.Id,
            PassengerName = "Jane Doe", PassengerEmail = "jane@example.com"
        });

        Assert.NotNull(result);
        Assert.StartsWith("TRN-", result.BookingReference);
        Assert.Equal("Jane Doe", result.PassengerName);
        Assert.Equal(train.Name, result.TrainName);
    }

    [Fact]
    public async Task CreateBooking_SeatAlreadyBooked_ThrowsInvalidOperation()
    {
        using var ctx = CreateContext();
        var service = new BookingService(ctx);
        var train = ctx.Trains.First();
        var seat = ctx.Seats.First(s => s.TrainId == train.Id);

        await service.CreateBookingAsync(new BookingRequest
        {
            TrainId = train.Id, SeatId = seat.Id,
            PassengerName = "A", PassengerEmail = "a@example.com"
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateBookingAsync(new BookingRequest
            {
                TrainId = train.Id, SeatId = seat.Id,
                PassengerName = "B", PassengerEmail = "b@example.com"
            }));
    }

    [Fact]
    public async Task CreateBooking_SeatBelongsToDifferentTrain_ThrowsInvalidOperation()
    {
        using var ctx = CreateContext();
        var service = new BookingService(ctx);
        var train1 = ctx.Trains.OrderBy(t => t.Id).First();
        var train2 = ctx.Trains.OrderBy(t => t.Id).Skip(1).First();
        var seatFromTrain2 = ctx.Seats.First(s => s.TrainId == train2.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateBookingAsync(new BookingRequest
            {
                TrainId = train1.Id, SeatId = seatFromTrain2.Id,
                PassengerName = "A", PassengerEmail = "a@example.com"
            }));
    }

    [Fact]
    public async Task CreateBooking_TrainNotFound_ThrowsKeyNotFound()
    {
        using var ctx = CreateContext();
        var service = new BookingService(ctx);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateBookingAsync(new BookingRequest
            {
                TrainId = 9999, SeatId = 1,
                PassengerName = "A", PassengerEmail = "a@example.com"
            }));
    }

    [Fact]
    public async Task GetBookingByReference_Exists_ReturnsBooking()
    {
        using var ctx = CreateContext();
        var service = new BookingService(ctx);
        var train = ctx.Trains.First();
        var seat = ctx.Seats.First(s => s.TrainId == train.Id);

        var created = await service.CreateBookingAsync(new BookingRequest
        {
            TrainId = train.Id, SeatId = seat.Id,
            PassengerName = "A", PassengerEmail = "a@example.com"
        });

        var result = await service.GetBookingByReferenceAsync(created.BookingReference);

        Assert.NotNull(result);
        Assert.Equal(created.BookingReference, result!.BookingReference);
    }

    [Fact]
    public async Task GetBookingByReference_NotFound_ReturnsNull()
    {
        using var ctx = CreateContext();
        var service = new BookingService(ctx);

        var result = await service.GetBookingByReferenceAsync("TRN-XXXXXX");

        Assert.Null(result);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
cd /Users/phunn/Documents/Code/Stock/TrainBooking
dotnet test --filter "BookingServiceTests" -v
```
Expected: FAIL — `BookingService` does not exist yet.

- [ ] **Step 3: Create IBookingService interface**

`TrainBooking.Api/Services/IBookingService.cs`:
```csharp
using TrainBooking.Api.DTOs;

namespace TrainBooking.Api.Services;

public interface IBookingService
{
    Task<BookingResponse> CreateBookingAsync(BookingRequest request);
    Task<BookingResponse?> GetBookingByReferenceAsync(string reference);
}
```

- [ ] **Step 4: Implement BookingService**

`TrainBooking.Api/Services/BookingService.cs`:
```csharp
using System.Data;
using Microsoft.EntityFrameworkCore;
using TrainBooking.Api.Data;
using TrainBooking.Api.DTOs;
using TrainBooking.Api.Models;

namespace TrainBooking.Api.Services;

public class BookingService : IBookingService
{
    private readonly AppDbContext _db;
    private static readonly char[] Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    public BookingService(AppDbContext db) => _db = db;

    public async Task<BookingResponse> CreateBookingAsync(BookingRequest request)
    {
        var train = await _db.Trains.FindAsync(request.TrainId)
            ?? throw new KeyNotFoundException($"Train {request.TrainId} not found.");

        var seat = await _db.Seats.FindAsync(request.SeatId)
            ?? throw new KeyNotFoundException($"Seat {request.SeatId} not found.");

        if (seat.TrainId != request.TrainId)
            throw new InvalidOperationException("Seat does not belong to the specified train.");

        // Use serializable transaction to prevent concurrent double-bookings in PostgreSQL.
        // InMemory provider does not support transactions; the IsBooked check below
        // still catches sequential double-bookings in tests.
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        // Re-fetch inside transaction to get the latest state
        var freshSeat = await _db.Seats.FindAsync(request.SeatId);
        if (freshSeat is null || freshSeat.IsBooked)
            throw new InvalidOperationException("Seat is already booked.");

        var reference = await GenerateUniqueReferenceAsync();

        var booking = new Booking
        {
            BookingReference = reference,
            TrainId = request.TrainId,
            SeatId = request.SeatId,
            PassengerName = request.PassengerName,
            PassengerEmail = request.PassengerEmail,
            BookedAt = DateTime.UtcNow
        };

        freshSeat.IsBooked = true;
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return MapToResponse(booking, train.Name, freshSeat);
    }

    public async Task<BookingResponse?> GetBookingByReferenceAsync(string reference)
    {
        var booking = await _db.Bookings
            .Include(b => b.Train)
            .Include(b => b.Seat)
            .FirstOrDefaultAsync(b => b.BookingReference == reference);

        if (booking is null) return null;

        return MapToResponse(booking, booking.Train.Name, booking.Seat);
    }

    private async Task<string> GenerateUniqueReferenceAsync()
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            var candidate = "TRN-" + new string(Enumerable
                .Range(0, 6)
                .Select(_ => Chars[Random.Shared.Next(Chars.Length)])
                .ToArray());

            if (!await _db.Bookings.AnyAsync(b => b.BookingReference == candidate))
                return candidate;
        }
        throw new InvalidOperationException("Failed to generate a unique booking reference after 5 attempts.");
    }

    private static BookingResponse MapToResponse(Booking booking, string trainName, Seat seat) => new()
    {
        BookingReference = booking.BookingReference,
        TrainName = trainName,
        Seat = $"{seat.Coach}-{seat.Number}",
        PassengerName = booking.PassengerName,
        PassengerEmail = booking.PassengerEmail,
        BookedAt = booking.BookedAt
    };
}
```

- [ ] **Step 5: Run tests**

```bash
cd /Users/phunn/Documents/Code/Stock/TrainBooking
dotnet test --filter "BookingServiceTests" -v
```
Expected: All 6 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add TrainBooking.Api/Services/ TrainBooking.Tests/BookingServiceTests.cs
git commit -m "feat: implement BookingService with serializable transaction and reference generation"
```

---

## Task 7: TrainsController

**Files:**
- Create: `TrainBooking.Api/Controllers/TrainsController.cs`
- Create: `TrainBooking.Tests/TrainsControllerTests.cs`

- [ ] **Step 1: Write failing tests**

`TrainBooking.Tests/TrainsControllerTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using TrainBooking.Api.DTOs;
using TrainBooking.Tests.Infrastructure;

namespace TrainBooking.Tests;

public class TrainsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TrainsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTrains_ReturnsOkWithList()
    {
        var response = await _client.GetAsync("/api/trains");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var trains = await response.Content.ReadFromJsonAsync<List<TrainDto>>();
        Assert.NotNull(trains);
        Assert.True(trains!.Count >= 1);
    }

    [Fact]
    public async Task GetTrain_ValidId_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/trains/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var train = await response.Content.ReadFromJsonAsync<TrainDto>();
        Assert.NotNull(train);
        Assert.Equal(1, train!.Id);
    }

    [Fact]
    public async Task GetTrain_InvalidId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/trains/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSeats_ValidTrain_ReturnsAvailableSeats()
    {
        var response = await _client.GetAsync("/api/trains/1/seats");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var seats = await response.Content.ReadFromJsonAsync<List<SeatDto>>();
        Assert.NotNull(seats);
        Assert.True(seats!.Count > 0);
    }

    [Fact]
    public async Task GetSeats_InvalidTrain_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/trains/9999/seats");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
cd /Users/phunn/Documents/Code/Stock/TrainBooking
dotnet test --filter "TrainsControllerTests" -v
```
Expected: FAIL — controller doesn't exist yet.

- [ ] **Step 3: Create TrainsController**

`TrainBooking.Api/Controllers/TrainsController.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrainBooking.Api.Data;
using TrainBooking.Api.DTOs;

namespace TrainBooking.Api.Controllers;

[ApiController]
[Route("api/trains")]
public class TrainsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TrainsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var trains = await _db.Trains
            .Select(t => new TrainDto
            {
                Id = t.Id, Name = t.Name, Origin = t.Origin,
                Destination = t.Destination,
                DepartureTime = t.DepartureTime, ArrivalTime = t.ArrivalTime
            })
            .ToListAsync();
        return Ok(trains);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var train = await _db.Trains
            .Where(t => t.Id == id)
            .Select(t => new TrainDto
            {
                Id = t.Id, Name = t.Name, Origin = t.Origin,
                Destination = t.Destination,
                DepartureTime = t.DepartureTime, ArrivalTime = t.ArrivalTime
            })
            .FirstOrDefaultAsync();

        if (train is null) return NotFound();
        return Ok(train);
    }

    [HttpGet("{id:int}/seats")]
    public async Task<IActionResult> GetAvailableSeats(int id)
    {
        var trainExists = await _db.Trains.AnyAsync(t => t.Id == id);
        if (!trainExists) return NotFound();

        var seats = await _db.Seats
            .Where(s => s.TrainId == id && !s.IsBooked)
            .Select(s => new SeatDto { Id = s.Id, Coach = s.Coach, Row = s.Row, Number = s.Number })
            .ToListAsync();

        return Ok(seats);
    }
}
```

- [ ] **Step 4: Wire up Program.cs (minimal, to make tests compile)**

`TrainBooking.Api/Program.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using TrainBooking.Api.Data;
using TrainBooking.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IBookingService, BookingService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program { }
```

- [ ] **Step 5: Run tests**

```bash
cd /Users/phunn/Documents/Code/Stock/TrainBooking
dotnet test --filter "TrainsControllerTests" -v
```
Expected: All 5 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add TrainBooking.Api/Controllers/TrainsController.cs TrainBooking.Api/Program.cs TrainBooking.Tests/TrainsControllerTests.cs
git commit -m "feat: add TrainsController with GET /trains, /trains/{id}, /trains/{id}/seats"
```

---

## Task 8: BookingsController

**Files:**
- Create: `TrainBooking.Api/Controllers/BookingsController.cs`
- Create: `TrainBooking.Tests/BookingsControllerTests.cs`

> Error responses use `Problem()` to produce RFC 7807 `ProblemDetails` format as required by spec.

- [ ] **Step 1: Write failing tests**

`TrainBooking.Tests/BookingsControllerTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using TrainBooking.Api.DTOs;
using TrainBooking.Tests.Infrastructure;

namespace TrainBooking.Tests;

public class BookingsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public BookingsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateBooking_ValidRequest_Returns201WithReference()
    {
        var request = new BookingRequest
        {
            TrainId = 1, SeatId = 1,
            PassengerName = "John Doe",
            PassengerEmail = "john@example.com"
        };

        var response = await _client.PostAsJsonAsync("/api/bookings", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.NotNull(result);
        Assert.StartsWith("TRN-", result!.BookingReference);
        Assert.Equal("John Doe", result.PassengerName);
    }

    [Fact]
    public async Task CreateBooking_MissingEmail_Returns400()
    {
        var request = new { TrainId = 1, SeatId = 1, PassengerName = "John" };
        var response = await _client.PostAsJsonAsync("/api/bookings", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_InvalidEmail_Returns400()
    {
        var request = new BookingRequest
        {
            TrainId = 1, SeatId = 2,
            PassengerName = "John", PassengerEmail = "not-an-email"
        };
        var response = await _client.PostAsJsonAsync("/api/bookings", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_TrainNotFound_Returns404()
    {
        var request = new BookingRequest
        {
            TrainId = 9999, SeatId = 1,
            PassengerName = "John", PassengerEmail = "john@example.com"
        };
        var response = await _client.PostAsJsonAsync("/api/bookings", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBooking_ValidReference_Returns200()
    {
        var createRequest = new BookingRequest
        {
            TrainId = 1, SeatId = 3,
            PassengerName = "Jane", PassengerEmail = "jane@example.com"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/bookings", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<BookingResponse>();

        var response = await _client.GetAsync($"/api/bookings/{created!.BookingReference}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.Equal(created.BookingReference, result!.BookingReference);
    }

    [Fact]
    public async Task GetBooking_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/bookings/TRN-XXXXXX");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
cd /Users/phunn/Documents/Code/Stock/TrainBooking
dotnet test --filter "BookingsControllerTests" -v
```
Expected: FAIL

- [ ] **Step 3: Create BookingsController**

`TrainBooking.Api/Controllers/BookingsController.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using TrainBooking.Api.DTOs;
using TrainBooking.Api.Services;

namespace TrainBooking.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService) => _bookingService = bookingService;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BookingRequest request)
    {
        try
        {
            var result = await _bookingService.CreateBookingAsync(request);
            return CreatedAtAction(nameof(GetByReference), new { reference = result.BookingReference }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    [HttpGet("{reference}")]
    public async Task<IActionResult> GetByReference(string reference)
    {
        var result = await _bookingService.GetBookingByReferenceAsync(reference);
        if (result is null) return NotFound();
        return Ok(result);
    }
}
```

- [ ] **Step 4: Run tests**

```bash
cd /Users/phunn/Documents/Code/Stock/TrainBooking
dotnet test --filter "BookingsControllerTests" -v
```
Expected: All 6 tests PASS.

- [ ] **Step 5: Run full test suite**

```bash
cd /Users/phunn/Documents/Code/Stock/TrainBooking
dotnet test -v
```
Expected: All tests PASS.

- [ ] **Step 6: Commit**

```bash
git add TrainBooking.Api/Controllers/BookingsController.cs TrainBooking.Tests/BookingsControllerTests.cs
git commit -m "feat: add BookingsController with POST /bookings and GET /bookings/{reference}"
```

---

## Task 9: EF Migrations & appsettings

**Files:**
- Modify: `TrainBooking.Api/appsettings.json`

- [ ] **Step 1: Update appsettings.json**

`TrainBooking.Api/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=trainbooking;Username=postgres;Password=postgres"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

- [ ] **Step 2: Add auto-migrate on startup to Program.cs**

In `Program.cs`, before `app.Run()`, add:
```csharp
// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
```

- [ ] **Step 3: Create initial EF migration**

```bash
cd /Users/phunn/Documents/Code/Stock/TrainBooking/TrainBooking.Api
dotnet ef migrations add InitialCreate
```
Expected: `Migrations/` folder created.

- [ ] **Step 4: Verify full test suite still passes**

```bash
cd /Users/phunn/Documents/Code/Stock/TrainBooking
dotnet test -v
```
Expected: All tests PASS.

- [ ] **Step 5: Commit**

```bash
git add TrainBooking.Api/appsettings.json TrainBooking.Api/Program.cs TrainBooking.Api/Migrations/
git commit -m "feat: add PostgreSQL connection string, auto-migrate on startup, and EF migrations"
```

---

## Task 10: Smoke Test Against PostgreSQL

> Requires a running PostgreSQL instance. Run this manually — not part of the automated test suite.

- [ ] **Step 1: Start PostgreSQL**

```bash
docker run -d --name trainbooking-pg \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=trainbooking \
  -p 5432:5432 postgres:16
```

- [ ] **Step 2: Run the API**

```bash
cd /Users/phunn/Documents/Code/Stock/TrainBooking/TrainBooking.Api
dotnet run
```
Expected: App starts, migrations applied, Swagger at `https://localhost:{port}/swagger`

- [ ] **Step 3: Smoke test GET /api/trains**

```bash
curl http://localhost:{port}/api/trains
```
Expected: JSON array with 2 trains.

- [ ] **Step 4: Smoke test POST /api/bookings**

```bash
curl -X POST http://localhost:{port}/api/bookings \
  -H "Content-Type: application/json" \
  -d '{"trainId":1,"seatId":1,"passengerName":"Test User","passengerEmail":"test@example.com"}'
```
Expected: 201 with `bookingReference` starting `TRN-`.

- [ ] **Step 5: Smoke test double-booking returns 409**

```bash
curl -X POST http://localhost:{port}/api/bookings \
  -H "Content-Type: application/json" \
  -d '{"trainId":1,"seatId":1,"passengerName":"Other User","passengerEmail":"other@example.com"}'
```
Expected: 409 Conflict with ProblemDetails body.

---

## Final Checklist

- [ ] All unit tests pass: `dotnet test`
- [ ] Swagger UI accessible in dev mode
- [ ] Migrations applied cleanly to PostgreSQL
- [ ] Double-booking returns 409 with ProblemDetails
- [ ] Invalid email returns 400
- [ ] Unknown train/seat returns 404
- [ ] Error responses follow RFC 7807 ProblemDetails format
