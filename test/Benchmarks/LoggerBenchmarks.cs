using BenchmarkDotNet.Attributes;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using Serilog;
using Logger = Serilog.Core.Logger;

namespace Benchmarks
{
    [JsonExporterAttribute.Full]
    public class LoggerBenchmarks
    {
        private Logger _serilog;
        private NLog.Logger _nlog;

        private const string LogMessage = "message with params: {param1}{param2}{param3}";

        private const string SerilogFile = "serilog.txt";
        private const string NLogFile = "nlog.txt";

        [GlobalSetup]
        public void Setup()
        {
            _serilog = new LoggerConfiguration()
                .WriteTo.File(SerilogFile, buffered: true, flushToDiskInterval: TimeSpan.FromMilliseconds(1000))
                .CreateLogger();

            var config = new LoggingConfiguration();
            var fileTarget = new FileTarget
            {
                Name = "FileTarget",
                FileName = NLogFile
            };
            var target = new BufferingTargetWrapper()
            {
                BufferSize = 100,
                SlidingTimeout = false,
                FlushTimeout = 1000,
                Name = "BufferedTarget",
                WrappedTarget = fileTarget
            };

            config.AddTarget("file", target);
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, target));
            LogManager.Configuration = config;
            LogManager.ReconfigExistingLoggers();
            _nlog = LogManager.GetCurrentClassLogger();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            File.Delete(SerilogFile);
            File.Delete(NLogFile);
        }

        [Benchmark]
        public void Serilog_Buffered()
        {
            _serilog.Information(LogMessage, 0, 1, 2);
        }

        [Benchmark]
        public void NLog_Buffered()
        {
            _nlog.Info(LogMessage, 0, 1, 2);
        }
    }
}
