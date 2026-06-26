namespace AiCustomerService.Infrastructure.AI.Stt;

/// <summary>未配置 STT 时使用的占位实现 — 返回固定提示让流程跑通</summary>
public class NoopSttProvider : IAiSttProvider
{
    public string ProviderName => "noop";

    public async Task<SttResult> RecognizeAsync(Stream audio, string format, CancellationToken ct = default)
    {
        await Task.Delay(10, ct);
        return new SttResult(
            Text: "（语音识别未配置，请检查 STT Provider）",
            DurationSeconds: 0,
            Provider: ProviderName,
            LatencyMs: 10
        );
    }
}
