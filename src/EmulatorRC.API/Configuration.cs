﻿using App.Metrics;
using App.Metrics.AspNetCore;
using App.Metrics.Extensions.Configuration;
using App.Metrics.Filtering;
using App.Metrics.Formatters.Ascii;
using App.Metrics.Formatters.Prometheus;
using EmulatorRC.API.Services.Diagnostics;

namespace EmulatorRC.API
{
    public static class Configuration
    {
        public static IServiceCollection AddDiagnostics(this WebApplicationBuilder builder)
        {
            var configuration = builder.Configuration;

            var filter = new MetricsFilter()
                .WhereContext(name => name != "Application.HttpRequests");
            var metricsBuilder = AppMetrics.CreateDefaultBuilder()
                .Configuration.ReadFrom(configuration)
                .Filter.With(filter);

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

            builder.Services.AddHostedService<AppMetricsCollector>();

            return builder.Services;
        }
    }
}