using System.Net;
using System.Net.Http.Json;
using TrainBooking.Api.DTOs;
using TrainBooking.Tests.Infrastructure;

namespace TrainBooking.Tests;

public class TrainsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TrainsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTrains_ReturnsOkWithList()
    {
        var response = await _client.GetAsync("/api/trains");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var trains = await response.Content.ReadFromJsonAsync<List<TrainDto>>();
        Assert.NotNull(trains);
        Assert.True(trains!.Count >= 1);
    }

    [Fact]
    public async Task GetTrain_ValidId_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/trains/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var train = await response.Content.ReadFromJsonAsync<TrainDto>();
        Assert.NotNull(train);
        Assert.Equal(1, train!.Id);
    }

    [Fact]
    public async Task GetTrain_InvalidId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/trains/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSeats_ValidTrain_ReturnsAvailableSeats()
    {
        var response = await _client.GetAsync("/api/trains/1/seats");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var seats = await response.Content.ReadFromJsonAsync<List<SeatDto>>();
        Assert.NotNull(seats);
        Assert.True(seats!.Count > 0);
    }

    [Fact]
    public async Task GetSeats_InvalidTrain_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/trains/9999/seats");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
