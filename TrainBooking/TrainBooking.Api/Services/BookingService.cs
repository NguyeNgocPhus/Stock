using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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

        // Use serializable transaction for relational providers (PostgreSQL) to prevent concurrent
        // double-bookings. The InMemory provider does not support relational transactions;
        // the IsBooked check below still catches sequential double-bookings in tests.
        IDbContextTransaction? transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
        await using var __ = transaction;

        // Force a fresh SELECT from the database to bypass EF's identity cache.
        // Without this reload, FindAsync returns the cached entity and misses concurrent updates.
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

        return MapToResponse(booking, train.Name, seat);
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
