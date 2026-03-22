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
