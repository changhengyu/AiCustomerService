using System.Diagnostics;

namespace AiCustomerService.Infrastructure.Observability;

/// <summary>全局 ActivitySource — 业务 span 入口</summary>
public static class AppActivitySource
{
    public const string Name = "AiCustomerService";
    public static readonly ActivitySource Source = new(Name, "1.0.0");
}
