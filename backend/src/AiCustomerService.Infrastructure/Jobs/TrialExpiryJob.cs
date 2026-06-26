using AiCustomerService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiCustomerService.Infrastructure.Jobs;

/// <summary>每日扫描 — 把过期 trial 租户置 expired</summary>
public class TrialExpiryJob
{
    private readonly AppDbContext _db;
    private readonly AiCustomerService.Infrastructure.Services.OpenApiService _webhooks;
    private readonly ILogger<TrialExpiryJob> _log;

    public TrialExpiryJob(AppDbContext db, AiCustomerService.Infrastructure.Services.OpenApiService webhooks, ILogger<TrialExpiryJob> log)
    { _db = db; _webhooks = webhooks; _log = log; }

    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var expired = await _db.Tenants
            .Where(t => t.Status == "active" && t.TrialEndsAt != null && t.TrialEndsAt < now && t.Plan == "trial")
            .ToListAsync(ct);

        foreach (var t in expired)
        {
            t.Status = "expired";
            t.Plan = "free";
            await _webhooks.PublishAsync(t.Id, "subscription.expired", new
            {
                tenantId = t.Id,
                reason = "trial_expired"
            }, ct);
            _log.LogInformation("Trial 过期: Tenant={Id}", t.Id);
        }
        await _db.SaveChangesAsync(ct);
        return expired.Count;
    }
}
