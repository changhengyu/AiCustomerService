using AiCustomerService.Core.Enums;

namespace AiCustomerService.Infrastructure.Payments;

/// <summary>沙箱/开发环境用 Noop — 直接返回成功 URL</summary>
public class NoopPaymentProvider : IPaymentProvider
{
    public PaymentProvider Provider => PaymentProvider.Noop;

    public Task<CheckoutResult> CreateCheckoutAsync(CheckoutRequest request, CancellationToken ct = default)
        => Task.FromResult(new CheckoutResult(
            CheckoutUrl: $"/api/v1/billing/noop/checkout?plan={request.Plan}",
            ProviderReference: $"noop_{Guid.NewGuid():N}",
            ProviderName: "noop",
            ExpiresAt: DateTime.UtcNow.AddHours(1)
        ));

    public Task<PaymentEvent> VerifyWebhookAsync(
        string rawBody, IDictionary<string, string> headers, CancellationToken ct = default)
        => Task.FromResult(new PaymentEvent(
            Success: true,
            ProviderName: "noop",
            ProviderReference: "noop_sandbox",
            Plan: "pro"
        ));
}
