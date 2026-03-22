using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using TrainBooking.Api.Metrics;

namespace TrainBooking.Tests;

public class BookingMetricsTests
{
    private static (BookingMetrics metrics, MetricCollector<int> bookingsCollector, MetricCollector<int> seatsCollector) CreateSut()
    {
        var services = new ServiceCollection();
        services.AddMetrics();
        var provider = services.BuildServiceProvider();
        var meterFactory = provider.GetRequiredService<IMeterFactory>();

        // Collectors are created before BookingMetrics intentionally:
        // MetricCollector subscribes via IMeterFactory and will capture measurements
        // from any Meter named "TrainBooking" created by BookingMetrics below.
        var bookingsCollector = new MetricCollector<int>(meterFactory, "TrainBooking", "trainbooking.bookings.created");
        var seatsCollector = new MetricCollector<int>(meterFactory, "TrainBooking", "trainbooking.seats.booked");
        var metrics = new BookingMetrics(meterFactory);

        return (metrics, bookingsCollector, seatsCollector);
    }

    [Fact]
    public void RecordBookingCreated_IncrementsCounter()
    {
        var (metrics, bookingsCollector, _) = CreateSut();

        metrics.RecordBookingCreated("42");

        var measurement = Assert.Single(bookingsCollector.GetMeasurementSnapshot());
        Assert.Equal(1, measurement.Value);
        Assert.Equal("42", measurement.Tags["train.id"]);
    }

    [Fact]
    public void RecordSeatBooked_IncrementsSeatsCounter()
    {
        var (metrics, _, seatsCollector) = CreateSut();

        metrics.RecordSeatBooked("42");

        var measurement = Assert.Single(seatsCollector.GetMeasurementSnapshot());
        Assert.Equal(1, measurement.Value);
        Assert.Equal("42", measurement.Tags["train.id"]);
    }
}
