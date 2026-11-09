using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

namespace Fleet.Shared.Observability.Extensions;

/// <summary>
/// Extension methods to register Serilog structured logging and OpenTelemetry tracing.
/// </summary>
public static class ObservabilityExtensions
{
    public static IHostBuilder UseFleetObservability(this IHostBuilder hostBuilder, string serviceName)
    {
        // 1. Configure Serilog Structured Logging
        hostBuilder.UseSerilog((context, services, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", serviceName)
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{ServiceName}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}",
                    formatProvider: CultureInfo.InvariantCulture);
        });

        // 2. Configure OpenTelemetry Distributed Tracing & Correlation Context
        hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddScoped<CorrelationContext>();
            services.AddScoped<ICorrelationContext>(sp => sp.GetRequiredService<CorrelationContext>());

            services.AddOpenTelemetry()
                .WithTracing(tracing =>
                {
                    tracing
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation();

                    var otlpEndpoint = context.Configuration["OpenTelemetry:OtlpEndpoint"]
                        ?? "http://localhost:4317";

                    tracing.AddOtlpExporter(opt =>
                    {
                        opt.Endpoint = new Uri(otlpEndpoint);
                    });
                });
        });

        return hostBuilder;
    }
}