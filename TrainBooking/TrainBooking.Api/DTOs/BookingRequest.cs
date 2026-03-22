using System.ComponentModel.DataAnnotations;

namespace TrainBooking.Api.DTOs;

public class BookingRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "TrainId must be a positive integer.")]
    public int TrainId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "SeatId must be a positive integer.")]
    public int SeatId { get; set; }

    [Required]
    public string PassengerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string PassengerEmail { get; set; } = string.Empty;
}
