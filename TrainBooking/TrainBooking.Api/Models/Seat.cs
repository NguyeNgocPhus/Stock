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
