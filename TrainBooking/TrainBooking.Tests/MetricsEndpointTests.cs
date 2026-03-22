using System.Net;
using TrainBooking.Tests.Infrastructure;

namespace TrainBooking.Tests;

public class MetricsEndpointTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GetMetrics_ReturnsOk()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/metrics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMetrics_ReturnsPrometheusContentType()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/metrics");
        var contentType = response.Content.Headers.ContentType?.MediaType;
        Assert.StartsWith("text/plain", contentType ?? string.Empty);
    }
}
