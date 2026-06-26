using System.Security.Cryptography;
using System.Text;
using AiCustomerService.Core.Configuration;
using AiCustomerService.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace AiCustomerService.Infrastructure.Payments;

/// <summary>Stripe 支付 — Checkout Session + Webhook 验签</summary>
public class StripePaymentProvider : IPaymentProvider
{
    public PaymentProvider Provider => PaymentProvider.Stripe;

    private readonly StripeOptions _opts;
    private readonly ILogger<StripePaymentProvider> _log;

    public StripePaymentProvider(IOptions<StripeOptions> opts, ILogger<StripePaymentProvider> log)
    { _opts = opts.Value; _log = log; }

    public async Task<CheckoutResult> CreateCheckoutAsync(CheckoutRequest request, CancellationToken ct = default)
    {
        var client = new StripeClient(_opts.SecretKey);
        var service = new SessionService(client);
        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.Currency,
                        Recurring = new SessionLineItemPriceDataRecurringOptions { Interval = "month" },
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"AI 客服 {request.Plan} 套餐"
                        },
                        UnitAmount = (long)request.AmountCents
                    },
                    Quantity = 1
                }
            },
            SuccessUrl = request.SuccessUrl ?? "https://example.com/billing/success",
            CancelUrl = request.CancelUrl ?? "https://example.com/billing/cancel",
            Metadata = new Dictionary<string, string>
            {
                ["tenant_id"] = request.TenantId.ToString(),
                ["plan"] = request.Plan
            }
        };
        var session = await service.CreateAsync(options, cancellationToken: ct);
        return new CheckoutResult(session.Url, session.Id, "stripe", DateTime.UtcNow.AddHours(2));
    }

    public Task<PaymentEvent> VerifyWebhookAsync(
        string rawBody, IDictionary<string, string> headers, CancellationToken ct = default)
    {
        var sigHeader = headers.TryGetValue("Stripe-Signature", out var s) ? s : null;
        if (string.IsNullOrEmpty(sigHeader))
            return Task.FromResult(new PaymentEvent(false, "stripe", "", "", "Missing signature"));

        try
        {
            var whSecret = _opts.WebhookSecret;
            var stripeEvent = EventUtility.ConstructEvent(rawBody, sigHeader, whSecret);

            if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
            {
                var session = (Session)stripeEvent.Data.Object;
                var plan = session.Metadata?.GetValueOrDefault("plan") ?? "pro";
                return Task.FromResult(new PaymentEvent(true, "stripe", session.Id, plan));
            }
            return Task.FromResult(new PaymentEvent(false, "stripe", "", "", $"Unhandled event: {stripeEvent.Type}"));
        }
        catch (StripeException ex)
        {
            _log.LogWarning(ex, "Stripe webhook 验签失败");
            return Task.FromResult(new PaymentEvent(false, "stripe", "", "", ex.Message));
        }
    }
}

public class StripeOptions
{
    public const string SectionName = "Stripe";
    public string SecretKey { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}
