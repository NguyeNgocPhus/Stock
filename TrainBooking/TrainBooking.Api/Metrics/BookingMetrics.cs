using System.Diagnostics.Metrics;

namespace TrainBooking.Api.Metrics;

public sealed class BookingMetrics : IBookingMetrics
{
    private readonly Counter<int> _bookingsCreated;
    private readonly Counter<int> _seatsBooked;

    public BookingMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(new MeterOptions("TrainBooking"));
        _bookingsCreated = meter.CreateCounter<int>(
            "trainbooking.bookings.created",
            description: "Total number of bookings successfully created.");
        _seatsBooked = meter.CreateCounter<int>(
            "trainbooking.seats.booked",
            description: "Total number of seats booked.");
    }

    public void RecordBookingCreated(string trainId) =>
        _bookingsCreated.Add(1, new KeyValuePair<string, object?>("train.id", trainId));

    public void RecordSeatBooked(string trainId) =>
        _seatsBooked.Add(1, new KeyValuePair<string, object?>("train.id", trainId));
}
