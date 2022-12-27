using App.Metrics.Filtering;
using App.Metrics;
using App.Metrics.AspNetCore;
using App.Metrics.Formatters.Ascii;
using App.Metrics.Formatters.Json;
using App.Metrics.Formatters.Prometheus;
using EmulatorHub.API.Services.Diagnostics;

namespace EmulatorHub.API
{
    public static class Configuration
    {
        private static IServiceCollection AddApplicationMetrics(this IServiceCollection services)
        {
            services.AddHostedService<AppMetricsCollector>();

            return services;
        }

        public static IServiceCollection AddDiagnostics(this WebApplicationBuilder builder)
        {
            //channels
            builder.Services.AddApplicationMetrics();

            var configuration = builder.Configuration;

            var filter = new MetricsFilter()
                .WhereContext(name => name != "appmetrics.internal");

            var metricsBuilder = AppMetrics.CreateDefaultBuilder();
#if DEBUG
            const string metricsFilePathOption = "MetricsOptions:FilePath";
            var filePath = configuration.GetValue<string>(metricsFilePathOption);

            if (!string.IsNullOrEmpty(filePath))
            {
                metricsBuilder.Report.ToTextFile(
                    options =>
                    {
                        options.MetricsOutputFormatter = new MetricsTextOutputFormatter();
                        options.AppendMetricsToTextFile = false;
                        options.Filter = filter;
                        options.FlushInterval = TimeSpan.FromSeconds(60);
                        options.OutputPathAndFileName = filePath;
                    });
            }
#endif
            var metrics = metricsBuilder.Build();

            builder.Services
                .AddMetrics(metrics);

            builder.Host
                .UseMetrics(metricsWebHostOptions =>
                {
                    metricsWebHostOptions.EndpointOptions = metricEndpointsOptions =>
                    {
                        metricEndpointsOptions.MetricsEndpointOutputFormatter = new MetricsTextOutputFormatter();
                        metricEndpointsOptions.MetricsTextEndpointOutputFormatter =
                            new MetricsPrometheusTextOutputFormatter();

                        metricEndpointsOptions.EnvironmentInfoEndpointEnabled = false;
                    };
                });

            return builder.Services;
        }
    }
}
