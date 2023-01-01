using App.Metrics;
using App.Metrics.Gauge;

namespace EmulatorHub.API.Services.Diagnostics
{
    public class AppMetricsRegistry
    {
        public static class Channels
        {
            private const string Context = "application_channels";
            public static GaugeOptions Length => new()
            {
                Context = Context,
                Name = "length",
                MeasurementUnit = Unit.Items
            };

            public static GaugeOptions OperationCount => new()
            {
                Context = Context,
                Name = "operation_count", 
                MeasurementUnit = Unit.Calls
            };

            public static GaugeOptions OperationTime => new()
            {
                Context = Context,
                Name = "operation_time_ms",
                MeasurementUnit = Unit.None
            };

            public static GaugeOptions DroppedItems => new()
            {
                Context = Context,
                Name = "dropped_items",
                MeasurementUnit = Unit.Items
            };
        }

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
