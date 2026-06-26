using AiCustomerService.Core.Enums;

namespace AiCustomerService.Infrastructure.Payments;

public record CheckoutRequest(
    Guid TenantId,
    string Plan,
    decimal AmountCents,
    string Currency = "cny",
    string? SuccessUrl = null,
    string? CancelUrl = null
);

public record CheckoutResult(
    string CheckoutUrl,
    string ProviderReference,
    string ProviderName,
    DateTime ExpiresAt
);

public record PaymentEvent(
    bool Success,
    string ProviderName,
    string ProviderReference,
    string Plan,
    string? FailureReason = null
);

/// <summary>支付提供方抽象</summary>
public interface IPaymentProvider
{
    PaymentProvider Provider { get; }
    Task<CheckoutResult> CreateCheckoutAsync(CheckoutRequest request, CancellationToken ct = default);
    Task<PaymentEvent> VerifyWebhookAsync(string rawBody, IDictionary<string, string> headers, CancellationToken ct = default);
}
