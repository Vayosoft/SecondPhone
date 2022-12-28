using App.Metrics;
using App.Metrics.Gauge;

namespace EmulatorHub.API.Services.Diagnostics
{
    public class AppMetricsRegistry
    {
        public static class Channels
        {
            private const string Context = "channel.";

            public static GaugeOptions Length(string context)
            {
                return new GaugeOptions
                {
                    Context = Context + context,
                    Name = "length",
                    MeasurementUnit = Unit.Items
                };
            }

            public static GaugeOptions OperationCount(string context)
            {
                return new GaugeOptions
                {
                    Context = Context + context,
                    Name = "operation_count",
                    MeasurementUnit = Unit.Calls
                };
            }

            public static GaugeOptions OperationTime(string context)
            {
                return new GaugeOptions
                {
                    Context = Context + context,
                    Name = "operation_time_ms",
                    MeasurementUnit = Unit.None
                };
            }

        }
    }
}
