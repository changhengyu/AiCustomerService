using AiCustomerService.Core.Interfaces;
using AiCustomerService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AiCustomerService.Infrastructure.Services;

/// <summary>
/// 商业智能（BI）统计服务。
/// 提供 Dashboard 所需的关键指标、会话趋势、热门问题等。
/// </summary>
public class BiService
{
    private readonly AppDbContext _db;

    public BiService(AppDbContext db) { _db = db; }

    /// <summary>Dashboard 概览</summary>
    public async Task<DashboardOverviewDto> GetOverviewAsync(Guid tenantId, int days = 30, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-days);

        // 总会话数 / AI 处理 / 人工 / 关闭
        var conversations = await _db.Conversations
            .Where(c => c.TenantId == tenantId && c.CreatedAt >= since)
            .Select(c => c.Status)
            .ToListAsync(ct);

        var total = conversations.Count;
        var active = conversations.Count(s => s == "active");
        var human = conversations.Count(s => s == "human");
        var closed = conversations.Count(s => s == "closed");

        // 总客户数
        var customerCount = await _db.Customers
            .Where(c => c.TenantId == tenantId)
            .CountAsync(ct);

        // 知识库文档数
        var docCount = await _db.KnowledgeDocuments
            .Where(d => d.TenantId == tenantId)
            .CountAsync(ct);

        // 消息总数 + AI 节省时间（按平均人工 5 分钟/会话估算）
        var messageCount = await _db.Messages
            .Where(m => m.Conversation.TenantId == tenantId && m.CreatedAt >= since)
            .CountAsync(ct);
        var aiHandled = active + closed;
        var minutesSaved = aiHandled * 5;

        return new DashboardOverviewDto(
            TotalConversations: total,
            ActiveConversations: active,
            HumanConversations: human,
            ClosedConversations: closed,
            AiHandledConversations: aiHandled,
            TotalCustomers: customerCount,
            TotalDocuments: docCount,
            TotalMessages: messageCount,
            EstimatedMinutesSaved: minutesSaved,
            PeriodDays: days
        );
    }

    /// <summary>会话趋势（按天）</summary>
    public async Task<List<TrendPointDto>> GetConversationTrendAsync(
        Guid tenantId, int days = 7, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.Date.AddDays(-days + 1);
        var data = await _db.Conversations
            .Where(c => c.TenantId == tenantId && c.CreatedAt >= since)
            .GroupBy(c => c.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        // 补全缺失日期
        var result = new List<TrendPointDto>();
        for (int i = 0; i < days; i++)
        {
            var d = since.AddDays(i);
            var point = data.FirstOrDefault(x => x.Date == d);
            result.Add(new TrendPointDto(d.ToString("MM-dd"), point?.Count ?? 0));
        }
        return result;
    }

    /// <summary>意向度分布</summary>
    public async Task<List<DistributionPointDto>> GetIntentionDistributionAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        var data = await _db.Customers
            .Where(c => c.TenantId == tenantId)
            .GroupBy(c => c.IntentionLevel)
            .Select(g => new { Level = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var total = data.Sum(d => d.Count);
        return data.Select(d => new DistributionPointDto(
            Label: d.Level,
            Count: d.Count,
            Percent: total == 0 ? 0 : Math.Round((double)d.Count / total * 100, 1)
        )).ToList();
    }

    /// <summary>热门问题 Top N（从最近 N 天的用户消息中聚合）</summary>
    public async Task<List<HotQuestionDto>> GetHotQuestionsAsync(
        Guid tenantId, int topN = 10, int days = 7, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-days);

        // 简化实现：按 message.content 聚合高频文本
        var messages = await _db.Messages
            .Where(m => m.Conversation.TenantId == tenantId
                && m.Role == "user"
                && m.CreatedAt >= since
                && m.ContentType == "text")
            .Select(m => m.Content)
            .Where(c => c != null && c.Length >= 5 && c.Length <= 80)
            .ToListAsync(ct);

        // 简单归一化：去标点 + 截断到前 30 字
        var grouped = messages
            .Select(m => Normalize(m!))
            .Where(m => m.Length >= 5)
            .GroupBy(m => m)
            .Select(g => new HotQuestionDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(topN)
            .ToList();
        return grouped;
    }

    /// <summary>AI 用量统计</summary>
    public async Task<AiUsageSummaryDto> GetAiUsageAsync(
        Guid tenantId, int days = 30, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var logs = await _db.AiUsageLogs
            .Where(l => l.TenantId == tenantId && l.CreatedAt >= since)
            .Select(l => new { l.TotalTokens, l.LatencyMs })
            .ToListAsync(ct);

        return new AiUsageSummaryDto(
            TotalCalls: logs.Count,
            TotalTokens: logs.Sum(l => l.TotalTokens),
            AvgLatencyMs: logs.Count == 0 ? 0 : (int)logs.Average(l => l.LatencyMs),
            P95LatencyMs: logs.Count == 0 ? 0 : logs.OrderBy(l => l.LatencyMs).ElementAt((int)(logs.Count * 0.95)).LatencyMs,
            PeriodDays: days
        );
    }

    private static string Normalize(string text)
    {
        // 去除标点，截断到前 30 字
        var cleaned = new string(text.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray());
        cleaned = string.Join(' ', cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return cleaned.Length > 30 ? cleaned[..30] : cleaned;
    }
}

public record DashboardOverviewDto(
    int TotalConversations,
    int ActiveConversations,
    int HumanConversations,
    int ClosedConversations,
    int AiHandledConversations,
    int TotalCustomers,
    int TotalDocuments,
    int TotalMessages,
    int EstimatedMinutesSaved,
    int PeriodDays
);

public record TrendPointDto(string Date, int Count);

public record DistributionPointDto(string Label, int Count, double Percent);

public record HotQuestionDto(string Question, int Count);

public record AiUsageSummaryDto(
    int TotalCalls,
    int TotalTokens,
    int AvgLatencyMs,
    int P95LatencyMs,
    int PeriodDays
);
