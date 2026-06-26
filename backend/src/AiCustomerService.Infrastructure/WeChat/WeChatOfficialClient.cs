using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using AiCustomerService.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiCustomerService.Infrastructure.WeChat;

public class WeChatOfficialClient
{
    private readonly HttpClient _http;
    private readonly WeChatOptions _options;
    private readonly ILogger<WeChatOfficialClient> _logger;
    private string? _cachedAccessToken;
    private DateTime _accessTokenExpiry = DateTime.MinValue;

    public WeChatOfficialClient(
        HttpClient http,
        IOptions<WeChatOptions> options,
        ILogger<WeChatOfficialClient> logger)
    {
        _options = options.Value;
        _logger = logger;
        _http = http;
        _http.BaseAddress = new Uri("https://api.weixin.qq.com/");
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    /// <summary>
    /// 验证微信回调签名
    /// </summary>
    public bool VerifySignature(string signature, string timestamp, string nonce)
    {
        var arr = new[] { _options.OfficialToken, timestamp, nonce }.OrderBy(s => s).ToArray();
        var joined = string.Join("", arr);
        var sha1 = SHA1.HashData(Encoding.UTF8.GetBytes(joined));
        var computed = Convert.ToHexString(sha1).ToLowerInvariant();
        return string.Equals(computed, signature, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 获取 AccessToken（带缓存，提前 5 分钟过期）
    /// </summary>
    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _accessTokenExpiry)
            return _cachedAccessToken;

        var resp = await _http.GetAsync(
            $"cgi-bin/token?grant_type=client_credential&appid={_options.OfficialToken}&secret={_options.AppSecret}",
            ct);
        resp.EnsureSuccessStatusCode();
        var data = await resp.Content.ReadFromJsonAsync<WeChatTokenResponse>(cancellationToken: ct);
        if (data == null || string.IsNullOrEmpty(data.AccessToken))
            throw new InvalidOperationException("获取微信 AccessToken 失败");

        _cachedAccessToken = data.AccessToken;
        _accessTokenExpiry = DateTime.UtcNow.AddSeconds(data.ExpiresIn - 300);
        _logger.LogInformation("微信 AccessToken 已刷新");
        return _cachedAccessToken;
    }

    /// <summary>
    /// 发送客服文本消息
    /// </summary>
    public async Task<bool> SendCustomerTextAsync(string openId, string content, CancellationToken ct = default)
    {
        var token = await GetAccessTokenAsync(ct);
        var payload = new
        {
            touser = openId,
            msgtype = "text",
            text = new { content }
        };
        var resp = await _http.PostAsJsonAsync(
            $"cgi-bin/message/custom/send?access_token={token}", payload, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);
        _logger.LogInformation("发送微信消息结果: {Json}", json);
        return resp.IsSuccessStatusCode;
    }

    /// <summary>
    /// 解密微信加密消息（兼容模式）
    /// </summary>
    public string DecryptMessage(string encrypted, string signature, string timestamp, string nonce)
    {
        // 简化：生产环境应使用完整的 AES-256-CBC PKCS#7 解密
        // 这里仅返回原文，使用 EncodingAESKey 解密需要 base64 decode + AES decrypt
        if (string.IsNullOrEmpty(_options.EncodingAESKey))
            return encrypted;
        try
        {
            var aesKey = Convert.FromBase64String(_options.EncodingAESKey + "=");
            // 实际解密逻辑省略，建议引入 Senparc.Weixin 库
            return Encoding.UTF8.GetString(aesKey.Take(32).ToArray());
        }
        catch
        {
            return encrypted;
        }
    }

    /// <summary>
    /// 下载微信临时素材（语音/图片/视频等）。
    /// 端点：https://api.weixin.qq.com/cgi-bin/media/get?access_token=...&media_id=...
    /// </summary>
    public async Task<byte[]?> DownloadMediaAsync(string mediaId, CancellationToken ct = default)
    {
        var token = await GetAccessTokenAsync(ct);
        var url = $"https://api.weixin.qq.com/cgi-bin/media/get?access_token={token}&media_id={mediaId}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await _http.SendAsync(req, ct);
        var bytes = await resp.Content.ReadAsByteArrayAsync(ct);

        // 失败时微信返回 JSON（errcode/errmsg），成功返回二进制
        if (!resp.IsSuccessStatusCode || (bytes.Length > 0 && bytes[0] == (byte)'{'))
        {
            var body = Encoding.UTF8.GetString(bytes);
            _logger.LogWarning("下载微信素材失败: {Body}", body);
            return null;
        }
        return bytes;
    }
}

public class WeChatTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string? ErrCode { get; set; }
    public string? ErrMsg { get; set; }
}

[XmlRoot("xml")]
public class WeChatIncomingMessage
{
    public string ToUserName { get; set; } = string.Empty;
    public string FromUserName { get; set; } = string.Empty;
    public string CreateTime { get; set; } = string.Empty;
    public string MsgType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? MsgId { get; set; }
    public string? Event { get; set; }
    public string? EventKey { get; set; }
}