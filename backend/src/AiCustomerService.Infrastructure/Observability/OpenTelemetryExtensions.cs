using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace AiCustomerService.Infrastructure.Observability;

/// <summary>
/// 注册 OpenTelemetry — Tracing + Metrics。
/// 默认无 OTLP collector 时不报错（exporter 自适应）。
/// </summary>
public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddAppTelemetry(
        this IServiceCollection services, IConfiguration config)
    {
        var resource = ResourceBuilder.CreateDefault()
            .AddService(serviceName: AppActivitySource.Name, serviceVersion: "1.0.0");

        var otlpEndpoint = config["OpenTelemetry:OtlpEndpoint"];
        var enableConsole = config.GetValue("OpenTelemetry:ConsoleExporter", false);

        services.AddOpenTelemetry()
            .WithTracing(t =>
            {
                t.SetResourceBuilder(resource)
                    .AddSource(AppActivitySource.Name)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();

                if (!string.IsNullOrEmpty(otlpEndpoint))
                    t.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));

                if (enableConsole)
                    t.AddConsoleExporter();
            })
            .WithMetrics(m =>
            {
                m.SetResourceBuilder(resource)
                    .AddMeter(AppMeter.Name)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (!string.IsNullOrEmpty(otlpEndpoint))
                    m.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));

                if (enableConsole)
                    m.AddConsoleExporter();
            });

        return services;
    }
}
