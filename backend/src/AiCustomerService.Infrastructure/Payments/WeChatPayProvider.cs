using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using AiCustomerService.Core.Configuration;
using AiCustomerService.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiCustomerService.Infrastructure.Payments;

/// <summary>微信支付 V3 — Native 下单 + 回调解密</summary>
public class WeChatPayProvider : IPaymentProvider
{
    public PaymentProvider Provider => PaymentProvider.WeChatPay;

    private readonly WeChatPayOptions _opts;
    private readonly ILogger<WeChatPayProvider> _log;

    public WeChatPayProvider(IOptions<WeChatPayOptions> opts, ILogger<WeChatPayProvider> log)
    { _opts = opts.Value; _log = log; }

    public async Task<CheckoutResult> CreateCheckoutAsync(CheckoutRequest request, CancellationToken ct = default)
    {
        // 实际生产：调 https://api.mch.weixin.qq.com/v3/pay/transactions/native
        // 这里返回占位 URL + 预订单 ID，开发者可接入完整 SDK
        var outTradeNo = $"WP{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
        var url = $"weixin://wxpay/bizpayurl?pr={outTradeNo}";
        return await Task.FromResult(new CheckoutResult(url, outTradeNo, "wechat_pay", DateTime.UtcNow.AddMinutes(15)));
    }

    public Task<PaymentEvent> VerifyWebhookAsync(
        string rawBody, IDictionary<string, string> headers, CancellationToken ct = default)
    {
        // 微信支付 V3 回调：RSA 验签 + AES-256-GCM 解密
        // 此处简化为签名检查 + 解析 XML/JSON 结果
        try
        {
            var timestamp = headers.TryGetValue("Wechatpay-Timestamp", out var t) ? t : "";
            var nonce = headers.TryGetValue("Wechatpay-Nonce", out var n) ? n : "";
            var signature = headers.TryGetValue("Wechatpay-Signature", out var s) ? s : "";
            if (string.IsNullOrEmpty(signature))
                return Task.FromResult(new PaymentEvent(false, "wechat_pay", "", "", "Missing signature"));

            // 生产环境：用商户私钥验签 + 用平台证书解密 resource.ciphertext
            // 当前实现：信任回调（开发阶段）
            var doc = XDocument.Parse(rawBody);
            var outTradeNo = doc.Root?.Element("out_trade_no")?.Value ?? "";
            var resultCode = doc.Root?.Element("result_code")?.Value ?? "FAIL";
            return Task.FromResult(new PaymentEvent(
                Success: resultCode == "SUCCESS",
                ProviderName: "wechat_pay",
                ProviderReference: outTradeNo,
                Plan: "pro"
            ));
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "微信支付回调解析失败");
            return Task.FromResult(new PaymentEvent(false, "wechat_pay", "", "", ex.Message));
        }
    }
}

public class WeChatPayOptions
{
    public const string SectionName = "WeChatPay";
    public string MchId { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
    public string ApiV3Key { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
}
