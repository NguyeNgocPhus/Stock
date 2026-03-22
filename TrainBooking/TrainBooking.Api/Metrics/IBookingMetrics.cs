namespace TrainBooking.Api.Metrics;

public interface IBookingMetrics
{
    void RecordBookingCreated(string trainId);
    void RecordSeatBooked(string trainId);
}
