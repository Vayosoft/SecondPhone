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
        public static IServiceCollection AddChannelMetrics(this IServiceCollection services)
        {
            services.AddHostedService<MetricsCollector>();

            return services;
        }

        public static IServiceCollection AddDiagnostics(this WebApplicationBuilder builder)
        {
            var filter = new MetricsFilter();
            var metrics = AppMetrics.CreateDefaultBuilder()
                //.Report.ToTextFile(
                //    options =>
                //    {
                //        options.MetricsOutputFormatter = new MetricsTextOutputFormatter();
                //        options.AppendMetricsToTextFile = false;
                //        options.Filter = filter;
                //        options.FlushInterval = TimeSpan.FromSeconds(60);
                //        options.OutputPathAndFileName = @"../../metrics/metrics.txt";
                //    })
                .Build();

            builder.Services
                .AddMetrics(metrics);

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
