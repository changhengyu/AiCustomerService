using AiCustomerService.Infrastructure.Services;

namespace AiCustomerService.Infrastructure.Jobs;

/// <summary>每分钟执行一次 — 投递 pending 的 WebhookDelivery（Outbox 模式）</summary>
public class WebhookDispatchJob
{
    private readonly OpenApiService _svc;
    public WebhookDispatchJob(OpenApiService svc) { _svc = svc; }

    public Task<int> RunAsync(CancellationToken ct = default)
        => _svc.DispatchPendingAsync(50, ct);
}
