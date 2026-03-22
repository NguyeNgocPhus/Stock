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
