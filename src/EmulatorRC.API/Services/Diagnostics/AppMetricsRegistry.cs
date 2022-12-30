using App.Metrics;
using App.Metrics.Gauge;

namespace EmulatorRC.API.Services.Diagnostics
{
    public class AppMetricsRegistry
    {
        public static class Gauges
        {
            public static readonly string ContextName = "system_threads";

            public static GaugeOptions AvailableThreads => new()
            {
                Context = ContextName,
                Name = "available",
                MeasurementUnit = Unit.Items
            };

            public static GaugeOptions MinThreads => new()
            {
                Context = ContextName,
                Name = "min",
                MeasurementUnit = Unit.Items
            };

            public static GaugeOptions MaxThreads => new()
            {
                Context = ContextName,
                Name = "max",
                MeasurementUnit = Unit.Items
            };
        }
    }
}
