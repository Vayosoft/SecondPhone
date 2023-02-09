using App.Metrics;
using App.Metrics.Gauge;
using App.Metrics.Meter;

namespace EmulatorRC.API.Services.Diagnostics
{
    public class AppMetricsRegistry
    {
        public static class Gauges
        {
            public static readonly string ContextName = "system_threads";

            public static GaugeOptions WorkingThreads => new()
            {
                Context = ContextName,
                Name = "working",
                MeasurementUnit = Unit.Items
            };

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

        public static class Meters
        {
            public static readonly string ContextName = "device_rpc_controller";

            public static MeterOptions Screens => new()
            {
                Context = ContextName,
                Name = "screen hits",
                MeasurementUnit = Unit.Calls
            };
        }
    }
}
