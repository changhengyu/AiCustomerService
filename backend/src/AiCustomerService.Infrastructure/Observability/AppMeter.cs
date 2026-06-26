using System.Diagnostics.Metrics;

namespace AiCustomerService.Infrastructure.Observability;

/// <summary>全局 Meter + 业务指标</summary>
public static class AppMeter
{
    public const string Name = "AiCustomerService";
    public static readonly Meter Meter = new(Name, "1.0.0");

    /// <summary>累计 chat token 用量（按 role 标签：prompt/completion）</summary>
    public static readonly Counter<long> ChatTokens =
        Meter.CreateCounter<long>("aics_chat_tokens_total", "tokens", "Chat token 用量");

    /// <summary>chat 调用延迟（毫秒）</summary>
    public static readonly Histogram<double> ChatLatency =
        Meter.CreateHistogram<double>("aics_chat_latency_ms", "ms", "Chat 延迟");

    /// <summary>tool call 延迟（毫秒）</summary>
    public static readonly Histogram<double> ToolCallLatency =
        Meter.CreateHistogram<double>("aics_tool_call_latency_ms", "ms", "Tool call 延迟");

    /// <summary>检索命中数</summary>
    public static readonly Counter<long> RetrievalHits =
        Meter.CreateCounter<long>("aics_retrieval_hits_total", "hits", "RAG 检索命中数");

    /// <summary>语音识别调用次数（按 provider）</summary>
    public static readonly Counter<long> SttCalls =
        Meter.CreateCounter<long>("aics_stt_calls_total", "calls", "STT 调用次数");
}
