using System.Net;
using System.Net.Http.Json;
using TrainBooking.Api.DTOs;
using TrainBooking.Tests.Infrastructure;

namespace TrainBooking.Tests;

public class BookingsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public BookingsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateBooking_ValidRequest_Returns201WithReference()
    {
        var request = new BookingRequest
        {
            TrainId = 1, SeatId = 1,
            PassengerName = "John Doe",
            PassengerEmail = "john@example.com"
        };

        var response = await _client.PostAsJsonAsync("/api/bookings", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.NotNull(result);
        Assert.StartsWith("TRN-", result!.BookingReference);
        Assert.Equal("John Doe", result.PassengerName);
    }

    [Fact]
    public async Task CreateBooking_MissingEmail_Returns400()
    {
        var request = new { TrainId = 1, SeatId = 1, PassengerName = "John" };
        var response = await _client.PostAsJsonAsync("/api/bookings", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_InvalidEmail_Returns400()
    {
        var request = new BookingRequest
        {
            TrainId = 1, SeatId = 2,
            PassengerName = "John", PassengerEmail = "not-an-email"
        };
        var response = await _client.PostAsJsonAsync("/api/bookings", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_TrainNotFound_Returns404()
    {
        var request = new BookingRequest
        {
            TrainId = 9999, SeatId = 1,
            PassengerName = "John", PassengerEmail = "john@example.com"
        };
        var response = await _client.PostAsJsonAsync("/api/bookings", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBooking_ValidReference_Returns200()
    {
        var createRequest = new BookingRequest
        {
            TrainId = 1, SeatId = 3,
            PassengerName = "Jane", PassengerEmail = "jane@example.com"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/bookings", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<BookingResponse>();

        var response = await _client.GetAsync($"/api/bookings/{created!.BookingReference}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.Equal(created.BookingReference, result!.BookingReference);
    }

    [Fact]
    public async Task GetBooking_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/bookings/TRN-XXXXXX");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
