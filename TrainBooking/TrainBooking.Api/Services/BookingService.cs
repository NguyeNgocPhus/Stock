using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TrainBooking.Api.Data;
using TrainBooking.Api.DTOs;
using TrainBooking.Api.Metrics;
using TrainBooking.Api.Models;

namespace TrainBooking.Api.Services;

public class BookingService : IBookingService
{
    private readonly AppDbContext _db;
    private readonly ILogger<BookingService> _logger;
    private readonly IBookingMetrics _metrics;
    private static readonly char[] Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    public BookingService(AppDbContext db, ILogger<BookingService> logger, IBookingMetrics metrics)
    {
        _db = db;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<BookingResponse> CreateBookingAsync(BookingRequest request)
    {
        _logger.LogInformation("Creating booking for train {TrainId}, seat {SeatId}, passenger {PassengerName}",
            request.TrainId, request.SeatId, request.PassengerName);

        var train = await _db.Trains.FindAsync(request.TrainId)
            ?? throw new KeyNotFoundException($"Train {request.TrainId} not found.");

        var seat = await _db.Seats.FindAsync(request.SeatId)
            ?? throw new KeyNotFoundException($"Seat {request.SeatId} not found.");

        if (seat.TrainId != request.TrainId)
            throw new InvalidOperationException("Seat does not belong to the specified train.");

        IDbContextTransaction? transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
        await using var __ = transaction;

        await _db.Entry(seat).ReloadAsync();
        if (seat.IsBooked)
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

        seat.IsBooked = true;
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        if (transaction is not null)
            await transaction.CommitAsync();

        _logger.LogInformation("Booking {BookingReference} created for passenger {PassengerName}",
            reference, request.PassengerName);

        _metrics.RecordBookingCreated(request.TrainId.ToString());
        _metrics.RecordSeatBooked(request.TrainId.ToString());

        return MapToResponse(booking, train.Name, seat);
    }

    public async Task<BookingResponse?> GetBookingByReferenceAsync(string reference)
    {
        _logger.LogInformation("Looking up booking {BookingReference}", reference);

        var booking = await _db.Bookings
            .Include(b => b.Train)
            .Include(b => b.Seat)
            .FirstOrDefaultAsync(b => b.BookingReference == reference);

        if (booking is null)
        {
            _logger.LogWarning("Booking {BookingReference} not found", reference);
            return null;
        }

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

            _logger.LogWarning("Booking reference collision on attempt {Attempt}: {Candidate}", attempt + 1, candidate);
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
