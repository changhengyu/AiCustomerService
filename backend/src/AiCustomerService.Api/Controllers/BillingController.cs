using AiCustomerService.Core.Configuration;
using AiCustomerService.Core.Entities;
using AiCustomerService.Core.Enums;
using AiCustomerService.Infrastructure.Payments;
using AiCustomerService.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AiCustomerService.Api.Controllers;

[ApiController]
[Route("api/v1/billing")]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly PlanPolicyOptions _policies;
    private readonly SubscriptionService _subs;
    private readonly IEnumerable<IPaymentProvider> _providers;
    private readonly OpenApiService _openApi;
    private readonly ILogger<BillingController> _log;

    public BillingController(
        IOptions<PlanPolicyOptions> policies,
        SubscriptionService subs,
        IEnumerable<IPaymentProvider> providers,
        OpenApiService openApi,
        ILogger<BillingController> log)
    {
        _policies = policies.Value;
        _subs = subs;
        _providers = providers;
        _openApi = openApi;
        _log = log;
    }

    [HttpGet("plans")]
    [AllowAnonymous]
    public IActionResult Plans()
    {
        return Ok(_policies.Plans.Select(p => new
        {
            name = p.Key,
            price_cents = p.Value.PriceCents,
            monthly_message_quota = p.Value.MonthlyMessageQuota,
            chat_rate_limit = p.Value.ChatRateLimit,
            upload_rate_limit = p.Value.UploadRateLimit,
            max_documents = p.Value.MaxDocuments,
            max_agents = p.Value.MaxAgents
        }));
    }

    public record CheckoutRequestDto(string Plan, string Provider);

    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutResult>> Checkout([FromBody] CheckoutRequestDto req, CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        if (!_policies.Plans.TryGetValue(req.Plan, out var policy))
            throw new ArgumentException($"未知套餐：{req.Plan}");

        if (!Enum.TryParse<PaymentProvider>(req.Provider, true, out var providerEnum))
            providerEnum = PaymentProvider.Noop;

        // 路由到对应 Provider
        var provider = _providers.FirstOrDefault(p => p.Provider == providerEnum);
        if (provider == null) return BadRequest(new { message = $"Provider {req.Provider} 未启用" });

        var checkoutReq = new CheckoutRequest(
            TenantId: tenantId,
            Plan: req.Plan,
            AmountCents: policy.PriceCents
        );
        var result = await provider.CreateCheckoutAsync(checkoutReq, ct);
        return Ok(result);
    }

    /// <summary>支付回调（按 provider 路由）</summary>
    [HttpPost("webhook/{provider}")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(string provider, CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);

        if (!Enum.TryParse<PaymentProvider>(provider, true, out var providerEnum))
            return BadRequest(new { message = "未知 provider" });

        var impl = _providers.FirstOrDefault(p => p.Provider == providerEnum);
        if (impl == null) return BadRequest(new { message = "Provider 未启用" });

        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
        var evt = await impl.VerifyWebhookAsync(body, headers, ct);
        if (!evt.Success)
        {
            _log.LogWarning("支付回调失败: {Provider} {Reason}", provider, evt.FailureReason);
            return BadRequest(new { message = evt.FailureReason });
        }

        await _subs.HandlePaymentSuccessAsync(evt.ProviderReference, evt.Plan, ct);
        return Ok(new { received = true });
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<Subscription>>> History(CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        return Ok(await _subs.GetHistoryAsync(tenantId, ct));
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel(CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        return await _subs.CancelAsync(tenantId, ct) ? NoContent() : NotFound();
    }

    private Guid RequireTenantId()
    {
        var v = HttpContext.User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(v)) throw new UnauthorizedAccessException("未登录");
        return Guid.Parse(v);
    }
}
