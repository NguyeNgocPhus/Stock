namespace TrainBooking.Api.DTOs;

public class SeatDto
{
    public int Id { get; set; }
    public string Coach { get; set; } = string.Empty;
    public int Row { get; set; }
    public string Number { get; set; } = string.Empty;
}
