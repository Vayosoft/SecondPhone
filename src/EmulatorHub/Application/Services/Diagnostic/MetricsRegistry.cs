using App.Metrics;
using App.Metrics.Apdex;
using App.Metrics.Counter;
using App.Metrics.Gauge;
using App.Metrics.Histogram;
using App.Metrics.Infrastructure;
using App.Metrics.Meter;
using App.Metrics.ReservoirSampling.ExponentialDecay;
using App.Metrics.ReservoirSampling.SlidingWindow;
using App.Metrics.ReservoirSampling.Uniform;
using App.Metrics.Scheduling;
using App.Metrics.Timer;

namespace EmulatorHub.Application.Services.Diagnostic
{
    public static class MetricsRegistry
    {
        public static readonly string Context = "EmulatorHub";

        public static TimerOptions TimerUsingAlgorithmRReservoir = new()
        {
            Context = Context,
            Name = "uniform",
            Reservoir = () => new DefaultAlgorithmRReservoir()
        };

        public static TimerOptions TimerUsingExponentialForwardDecayingReservoir = new()
        {
            Context = Context,
            Name = "exponentially-decaying",
            Reservoir = () =>
                new DefaultForwardDecayingReservoir(AppMetricsReservoirSamplingConstants.DefaultSampleSize,
                    AppMetricsReservoirSamplingConstants.DefaultExponentialDecayFactor, 0.0, new StopwatchClock())
        };

        public static TimerOptions TimerUsingSlidingWindowReservoir = new()
        {
            Context = Context,
            Name = "sliding-window",
            Reservoir = () => new DefaultSlidingWindowReservoir()
        };

        public static TimerOptions TimerUsingForwardDecayingLowWeightThresholdReservoir =
            new()
            {
                Context = Context,
                Name = "exponentially-decaying-low-weight",
                Reservoir = () => new DefaultForwardDecayingReservoir(
                    AppMetricsReservoirSamplingConstants.DefaultSampleSize,
                    0.1, // Bias heavily towards lasst 15 seconds of sampling; disregard everything older than 40 seconds
                    0.001, // Samples with weight of less than 10% of average should be discarded when rescaling
                    new StopwatchClock(),
                    new DefaultReservoirRescaleScheduler(TimeSpan.FromSeconds(30)))
            };

        public static CounterOptions ActiveUserCounter => new()
        {
            Context = Context,
            Name = "Active User Counter",
            MeasurementUnit = Unit.Calls
        };

        public static MeterOptions CacheHitsMeter = new()
        {
            Context = Context,
            Name = "Req Hits",
            MeasurementUnit = Unit.Calls
        };

        public static HistogramOptions PostAndPutRequestSize = new()
        {
            Name = "Размер веб - запроса Post и Put",
            MeasurementUnit = Unit.Bytes
        };

        public static ApdexOptions SampleApdex = new()
        {
            Name = "Пример Apdex"
        };

        public static GaugeOptions CacheHitRatioGauge = new()
        {
            Name = "Cache Gauge",
            MeasurementUnit = Unit.Calls
        };
    }
}