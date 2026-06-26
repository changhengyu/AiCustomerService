namespace AiCustomerService.Infrastructure.AI.Stt;

public record SttResult(
    string Text,
    double DurationSeconds,
    string Provider,
    int LatencyMs
);

/// <summary>语音识别（Speech-to-Text）提供方抽象</summary>
public interface IAiSttProvider
{
    string ProviderName { get; }
    Task<SttResult> RecognizeAsync(Stream audio, string format, CancellationToken ct = default);
}
