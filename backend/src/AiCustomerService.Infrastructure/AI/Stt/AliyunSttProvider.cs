using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using AiCustomerService.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiCustomerService.Infrastructure.AI.Stt;

/// <summary>
/// 阿里云一句话识别（短语音 ≤ 60s）。
/// 协议：HMAC-SHA1 over canonical request → Token header。
/// 文档：https://help.aliyun.com/zh/isi/getting-started/restful-api-for-short-speech-recognition
/// </summary>
public class AliyunSttProvider : IAiSttProvider
{
    public string ProviderName => "aliyun";

    private readonly AliyunSttOptions _opts;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<AliyunSttProvider> _log;

    public AliyunSttProvider(
        IOptions<AliyunSttOptions> opts,
        IHttpClientFactory http,
        ILogger<AliyunSttProvider> log)
    { _opts = opts.Value; _http = http; _log = log; }

    public async Task<SttResult> RecognizeAsync(Stream audio, string format, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var client = _http.CreateClient("stt");
        client.Timeout = TimeSpan.FromSeconds(30);

        // 一句话识别：上传完整二进制 + query string 携带 token
        var token = GenerateToken();
        var url = $"https://{_opts.Endpoint}/stream/v1/asr?appkey={_opts.AppKey}" +
                  $"&format={format}&sample_rate=16000&enable_punctuation_prediction=true" +
                  $"&enable_inverse_text_normalization=true&token={token}";

        using var content = new StreamContent(audio);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        var resp = await client.PostAsync(url, content, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            _log.LogWarning("阿里云 STT 失败: {Status} {Body}", resp.StatusCode, body);
            throw new InvalidOperationException($"阿里云 STT 失败: {resp.StatusCode}");
        }

        // 简化解析：{"result":"text","status":20000000,...}
        var text = ExtractText(body);
        sw.Stop();
        return new SttResult(text, 0, ProviderName, (int)sw.ElapsedMilliseconds);
    }

    private string GenerateToken()
    {
        // 阿里云 NLS Token 算法：HMAC-SHA1 over canonical query
        // 当前实现：基础占位（生产应接入 Alibaba Cloud SDK 或完整签名）
        var bytes = Encoding.UTF8.GetBytes($"{_opts.AccessKeyId}:{_opts.AccessKeySecret}");
        var hash = SHA1.HashData(bytes);
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string ExtractText(string body)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("result", out var r))
                return r.GetString() ?? string.Empty;
        }
        catch { }
        return string.Empty;
    }
}

public class AliyunSttOptions
{
    public const string SectionName = "AliyunStt";
    public string Endpoint { get; set; } = "nls-gateway-cn-shanghai.aliyuncs.com";
    public string AppKey { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string AccessKeySecret { get; set; } = string.Empty;
}
