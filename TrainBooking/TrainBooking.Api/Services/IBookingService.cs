using TrainBooking.Api.DTOs;

namespace TrainBooking.Api.Services;

public interface IBookingService
{
    Task<BookingResponse> CreateBookingAsync(BookingRequest request);
    Task<BookingResponse?> GetBookingByReferenceAsync(string reference);
}
