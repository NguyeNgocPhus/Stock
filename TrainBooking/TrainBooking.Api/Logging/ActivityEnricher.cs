using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace TrainBooking.Api.Logging;

public class ActivityEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? "no-trace";
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", traceId));
    }
}
