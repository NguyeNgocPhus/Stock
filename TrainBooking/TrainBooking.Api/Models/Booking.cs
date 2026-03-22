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
