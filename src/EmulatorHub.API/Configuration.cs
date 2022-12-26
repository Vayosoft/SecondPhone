using App.Metrics.AspNetCore;
using App.Metrics.Filtering;
using App.Metrics;
using App.Metrics.Formatters.Ascii;
using App.Metrics.Formatters.Prometheus;
using EmulatorHub.API.Services.Diagnostics;

namespace EmulatorHub.API
{
    public static class Configuration
    {
        private static IServiceCollection AddChannelMetrics(this IServiceCollection services)
        {
            services.AddHostedService<MetricsCollector>();

            return services;
        }

        public static IServiceCollection AddDiagnostics(this WebApplicationBuilder builder)
        {
            //channels
            builder.Services.AddChannelMetrics();

            var configuration = builder.Configuration;

            var filter = new MetricsFilter();
            var metricsBuilder = AppMetrics.CreateDefaultBuilder();
#if DEBUG
            const string metricsFilePathOption = "MetricsOptions:FilePath";
            const string defaultFilePath = "../../metrics/metrics.txt";

            metricsBuilder.Report.ToTextFile(
            options =>
            {
                options.MetricsOutputFormatter = new MetricsTextOutputFormatter();
                options.AppendMetricsToTextFile = false;
                options.Filter = filter;
                options.FlushInterval = TimeSpan.FromSeconds(60);
                options.OutputPathAndFileName =
                    configuration.GetValue<string>(metricsFilePathOption) ?? defaultFilePath;
            });
#endif
            var metrics = metricsBuilder.Build();

            builder.Services
                .AddMetrics(metrics);
            builder.Services
                .AddAppMetricsCollectors();

            builder.Host
                .UseMetrics(metricsWebHostOptions =>
                {
                    metricsWebHostOptions.EndpointOptions = metricEndpointsOptions =>
                    {
                        metricEndpointsOptions.MetricsEndpointOutputFormatter = new MetricsPrometheusProtobufOutputFormatter();
                        metricEndpointsOptions.MetricsTextEndpointOutputFormatter = new MetricsPrometheusTextOutputFormatter();

                        metricEndpointsOptions.EnvironmentInfoEndpointEnabled = false;
                    };
                });

            return builder.Services;
        }
    }
}
