using AiCustomerService.Core.Configuration;
using AiCustomerService.Core.Entities;
using AiCustomerService.Core.Enums;
using AiCustomerService.Core.Exceptions;
using AiCustomerService.Infrastructure.Data;
using AiCustomerService.Infrastructure.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiCustomerService.Infrastructure.Services;

/// <summary>订阅生命周期：创建 / 续费 / 升级 / 取消 / 配额检查</summary>
public class SubscriptionService
{
    private readonly AppDbContext _db;
    private readonly PlanPolicyOptions _policies;
    private readonly OpenApiService _webhooks;
    private readonly ILogger<SubscriptionService> _log;

    public SubscriptionService(
        AppDbContext db,
        IOptions<PlanPolicyOptions> policies,
        OpenApiService webhooks,
        ILogger<SubscriptionService> log)
    {
        _db = db;
        _policies = policies.Value;
        _webhooks = webhooks;
        _log = log;
    }

    public async Task<CheckoutResult> CreateCheckoutAsync(
        Guid tenantId, string plan, PaymentProvider provider, CancellationToken ct = default)
    {
        if (!_policies.Plans.TryGetValue(plan, out var policy))
            throw new ArgumentException($"未知套餐：{plan}");

        var tenant = await _db.Tenants.FindAsync(new object[] { tenantId }, ct)
            ?? throw new ArgumentException("Tenant not found");

        var req = new CheckoutRequest(
            TenantId: tenantId,
            Plan: plan,
            AmountCents: policy.PriceCents
        );

        // 写 subscription 行（pending）
        var sub = new Subscription
        {
            TenantId = tenantId,
            Plan = plan,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            AmountCents = (long)policy.PriceCents,
            PaymentMethod = provider.ToString().ToLowerInvariant(),
            Status = "pending"
        };
        _db.Subscriptions.Add(sub);
        await _db.SaveChangesAsync(ct);

        // 委托给具体 provider（实际通过 DI 多注册实现路由）
        var providerImpl = provider switch
        {
            PaymentProvider.Stripe => (CheckoutResult?)null, // 由 controller 路由
            _ => new CheckoutResult("/api/v1/billing/noop/checkout", sub.Id.ToString(), "noop", DateTime.UtcNow.AddHours(1))
        };

        // 简化：当前实现用 Noop 占位（Stripe/微信支付集成在 BillingController 中按 provider 路由）
        return providerImpl ?? new CheckoutResult(
            $"/api/v1/billing/checkout/{sub.Id}",
            sub.Id.ToString(),
            provider.ToString().ToLowerInvariant(),
            DateTime.UtcNow.AddHours(2));
    }

    public async Task HandlePaymentSuccessAsync(string providerRef, string plan, CancellationToken ct = default)
    {
        var sub = await _db.Subscriptions.FirstOrDefaultAsync(
            s => s.Id.ToString() == providerRef || s.PaymentId == providerRef, ct);
        if (sub == null)
        {
            _log.LogWarning("Subscription not found for payment ref {Ref}", providerRef);
            return;
        }

        sub.Status = "active";
        sub.PaidAt = DateTime.UtcNow;
        sub.PaymentId = providerRef;
        sub.EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1));

        // 更新 tenant.plan
        var tenant = await _db.Tenants.FindAsync(new object[] { sub.TenantId }, ct);
        if (tenant != null)
        {
            tenant.Plan = plan;
            tenant.Status = "active";
            tenant.TrialEndsAt = null;
        }
        await _db.SaveChangesAsync(ct);

        // 触发 webhook
        await _webhooks.PublishAsync(sub.TenantId, "subscription.activated", new
        {
            tenantId = sub.TenantId,
            plan,
            subscriptionId = sub.Id,
            endDate = sub.EndDate
        }, ct);
    }

    public async Task<bool> CancelAsync(Guid tenantId, CancellationToken ct = default)
    {
        var sub = await _db.Subscriptions
            .Where(s => s.TenantId == tenantId && s.Status == "active")
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (sub == null) return false;

        sub.Status = "cancelled";
        var tenant = await _db.Tenants.FindAsync(new object[] { tenantId }, ct);
        if (tenant != null)
        {
            tenant.Plan = "free";
        }
        await _db.SaveChangesAsync(ct);
        await _webhooks.PublishAsync(tenantId, "subscription.cancelled", new { tenantId, subscriptionId = sub.Id }, ct);
        return true;
    }

    public async Task CheckQuotaAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FindAsync(new object[] { tenantId }, ct);
        if (tenant == null) return;

        if (_policies.Plans.TryGetValue(tenant.Plan, out var policy))
        {
            if (tenant.MonthlyMessageUsed >= policy.MonthlyMessageQuota)
                throw new QuotaExceededException("Tenant.QuotaExceeded");
        }
    }

    public async Task IncrementUsageAsync(Guid tenantId, int messageCount = 1, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FindAsync(new object[] { tenantId }, ct);
        if (tenant == null) return;
        tenant.MonthlyMessageUsed += messageCount;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<Subscription>> GetHistoryAsync(Guid tenantId, CancellationToken ct = default)
        => await _db.Subscriptions
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);
}
