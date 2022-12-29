﻿using App.Metrics.Filtering;
using App.Metrics;
using App.Metrics.AspNetCore;
using App.Metrics.Formatters.Ascii;
using App.Metrics.Formatters.Prometheus;
using EmulatorHub.API.Services.Diagnostics;
using App.Metrics.Extensions.Configuration;
using EmulatorHub.API.Model.Diagnostics;

namespace EmulatorHub.API
{
    public static class Configuration
    {
        public static IServiceCollection AddDiagnostics(this WebApplicationBuilder builder)
        {
            var configuration = builder.Configuration;

            //channels
            builder.Services.Configure<CollectorOptions>(configuration.GetSection("MetricsCollector"));
            builder.Services.AddHostedService<AppMetricsCollector>();

            var filter = new MetricsFilter()
                .WhereContext(name => name != "appmetrics.internal");

            var metricsBuilder = AppMetrics.CreateDefaultBuilder()
                .Configuration.ReadFrom(configuration)
                .Filter.With(filter);
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
                        options.FlushInterval = TimeSpan.FromSeconds(60);
                        options.OutputPathAndFileName = filePath;
                    });
            }
#endif
            var metrics = metricsBuilder.Build();

            builder.Services
                .AddMetrics(metrics)
                .AddAppMetricsSystemMetricsCollector()
                .AddAppMetricsGcEventsMetricsCollector();

            builder.Host
                .UseMetrics(metricsWebHostOptions =>
                {
                    metricsWebHostOptions.EndpointOptions = metricEndpointsOptions =>
                    {
                        metricEndpointsOptions.MetricsEndpointOutputFormatter = new MetricsPrometheusTextOutputFormatter();
                        metricEndpointsOptions.MetricsTextEndpointOutputFormatter =
                            new MetricsTextOutputFormatter();

                        metricEndpointsOptions.EnvironmentInfoEndpointEnabled = false;
                    };
                });

            return builder.Services;
        }
    }
}
