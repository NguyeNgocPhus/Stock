using System.Net;
using TrainBooking.Tests.Infrastructure;

namespace TrainBooking.Tests;

public class MetricsEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MetricsEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMetrics_WhenEndpointMapped_ReturnsOk()
    {
        var response = await _client.GetAsync("/metrics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMetrics_WhenEndpointMapped_ReturnsPrometheusContentType()
    {
        var response = await _client.GetAsync("/metrics");
        var contentType = response.Content.Headers.ContentType?.MediaType;
        Assert.NotNull(contentType);
        Assert.StartsWith("text/plain", contentType);
    }
}
