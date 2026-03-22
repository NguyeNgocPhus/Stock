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

        // Explicit relationship configuration
        modelBuilder.Entity<Train>()
            .HasMany(t => t.Seats)
            .WithOne(s => s.Train)
            .HasForeignKey(s => s.TrainId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Train>()
            .HasMany(t => t.Bookings)
            .WithOne(b => b.Train)
            .HasForeignKey(b => b.TrainId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Seat>()
            .HasMany<Booking>()
            .WithOne(b => b.Seat)
            .HasForeignKey(b => b.SeatId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed trains
        modelBuilder.Entity<Train>().HasData(
            new Train { Id = 1, Name = "Express 101", Origin = "Hanoi", Destination = "Ho Chi Minh City",
                DepartureTime = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc),
                ArrivalTime = new DateTime(2026, 4, 1, 20, 0, 0, DateTimeKind.Utc) },
            new Train { Id = 2, Name = "Express 202", Origin = "Ho Chi Minh City", Destination = "Da Nang",
                DepartureTime = new DateTime(2026, 4, 2, 7, 0, 0, DateTimeKind.Utc),
                ArrivalTime = new DateTime(2026, 4, 2, 15, 0, 0, DateTimeKind.Utc) }
        );

        // Seed seats: 2 trains x 2 coaches x 10 rows = 40 seats total
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
