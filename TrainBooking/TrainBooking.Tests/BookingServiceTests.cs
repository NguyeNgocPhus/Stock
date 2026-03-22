using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrainBooking.Api.Data;
using TrainBooking.Api.DTOs;
using TrainBooking.Api.Metrics;
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

    private static IBookingMetrics CreateMetrics()
    {
        var services = new ServiceCollection();
        services.AddMetrics();
        var provider = services.BuildServiceProvider();
        return new BookingMetrics(provider.GetRequiredService<IMeterFactory>());
    }

    [Fact]
    public async Task CreateBooking_ValidRequest_ReturnsBookingResponse()
    {
        using var ctx = CreateContext();
        var service = new BookingService(ctx, CreateMetrics());
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
        var service = new BookingService(ctx, CreateMetrics());
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
        var service = new BookingService(ctx, CreateMetrics());
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
        var service = new BookingService(ctx, CreateMetrics());

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
        var service = new BookingService(ctx, CreateMetrics());
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
        var service = new BookingService(ctx, CreateMetrics());

        var result = await service.GetBookingByReferenceAsync("TRN-XXXXXX");

        Assert.Null(result);
    }
}
