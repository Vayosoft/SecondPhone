using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.ReservoirSampling.Uniform;
using App.Metrics.Timer;

namespace EmulatorRC.API.Services.Diagnostics
{
    public class MetricsRegistry
    {
        public static TimerOptions TimerUsingAlgorithmRReservoir = new()
        {
            Name = "uniform",
            Reservoir = () => new DefaultAlgorithmRReservoir()
        };

        public static CounterOptions ActiveOuterConnections => new()
        {
            Context = "OuterHandler",
            Name = "Active Outer Connections",
            MeasurementUnit = Unit.Calls
        };

        public static CounterOptions ActiveInnerConnections => new()
        {
            Context = "InnerHandler",
            Name = "Active Inner Connections",
            MeasurementUnit = Unit.Calls
        };
    }
}
