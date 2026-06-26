using System.Text.Json;
using AiCustomerService.Core.Entities;
using AiCustomerService.Core.Exceptions;
using AiCustomerService.Core.Interfaces;
using AiCustomerService.Infrastructure.AI.RAG;
using AiCustomerService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiCustomerService.Infrastructure.Services;

/// <summary>
/// 轻量级 RAG 评测服务（基于 RAGAS 思想）。
/// 不依赖外部 RAGAS 库，使用 LLM 自身 + 启发式打分。
/// 适合内部回归测试与回归基线对比。
/// </summary>
public class EvaluationService : IEvaluationService
{
    private readonly AppDbContext _db;
    private readonly IAIService _ai;
    private readonly HybridRetriever _retriever;
    private readonly ITenantContext _tenantCtx;
    private readonly ILogger<EvaluationService> _logger;

    public EvaluationService(
        AppDbContext db,
        IAIService ai,
        HybridRetriever retriever,
        ITenantContext tenantCtx,
        ILogger<EvaluationService> logger)
    {
        _db = db;
        _ai = ai;
        _retriever = retriever;
        _tenantCtx = tenantCtx;
        _logger = logger;
    }

    public async Task<EvaluationReportDto> RunAsync(EvaluationRequestDto request, CancellationToken ct = default)
    {
        if (request.Cases == null || request.Cases.Count == 0)
            throw new ValidationException("评测用例不能为空");

        var startedAt = DateTime.UtcNow;
        var items = new List<EvalResultItemDto>();
        var tenant = await _db.Tenants.FindAsync(new object?[] { request.TenantId }, ct);
        var settings = tenant != null
            ? JsonSerializer.Deserialize<TenantSettingsDto>(tenant.Settings) ?? DefaultSettings()
            : DefaultSettings();

        foreach (var testCase in request.Cases)
        {
            // 1) 检索上下文
            string context = testCase.Context ?? string.Empty;
            if (string.IsNullOrEmpty(context))
            {
                var chunks = await _retriever.RetrieveAsync(
                    request.TenantId, testCase.Question, topK: 5, ct: ct);
                context = string.Join("\n", chunks.Select(c => c.Chunk.Content));
            }

            // 2) 生成答案
            var sysPrompt = $"{settings.SystemPrompt ?? "你是 AI 客服。"} \n" +
                $"请仅基于以下上下文回答用户问题：\n{context}";
            var aiResp = await _ai.ChatAsync(new Core.DTOs.AI.ChatRequest(
                TenantId: request.TenantId,
                Model: "qwen-plus",
                Messages: new List<Core.DTOs.AI.ChatMessage> { new("user", testCase.Question) },
                SystemPrompt: sysPrompt
            ), ct);

            // 3) 三指标打分
            var faithfulness = ScoreFaithfulness(aiResp.Content, context);
            var relevancy = ScoreAnswerRelevancy(aiResp.Content, testCase.Question);
            var precision = ScoreContextPrecision(context, testCase.Question, testCase.GroundTruthAnswer);

            items.Add(new EvalResultItemDto(
                Question: testCase.Question,
                GeneratedAnswer: aiResp.Content,
                ReferenceAnswer: testCase.GroundTruthAnswer,
                Faithfulness: faithfulness,
                AnswerRelevancy: relevancy,
                ContextPrecision: precision
            ));
        }

        var report = new EvalReport
        {
            TenantId = request.TenantId,
            DatasetName = request.DatasetName,
            TotalCases = items.Count,
            FaithfulnessAvg = items.Average(i => i.Faithfulness),
            AnswerRelevancyAvg = items.Average(i => i.AnswerRelevancy),
            ContextPrecisionAvg = items.Average(i => i.ContextPrecision),
            StartedAt = startedAt,
            CompletedAt = DateTime.UtcNow,
            Status = "completed",
            ItemsJson = JsonSerializer.Serialize(items)
        };
        _db.EvalReports.Add(report);
        await _db.SaveChangesAsync(ct);

        return new EvaluationReportDto(
            Id: report.Id,
            TenantId: report.TenantId,
            DatasetName: report.DatasetName,
            TotalCases: report.TotalCases,
            FaithfulnessAvg: report.FaithfulnessAvg,
            AnswerRelevancyAvg: report.AnswerRelevancyAvg,
            ContextPrecisionAvg: report.ContextPrecisionAvg,
            StartedAt: report.StartedAt,
            CompletedAt: report.CompletedAt,
            Status: report.Status,
            Items: items
        );
    }

    public async Task<EvaluationReportDto?> GetReportAsync(Guid reportId, CancellationToken ct = default)
    {
        var report = await _db.EvalReports.FindAsync(new object?[] { reportId }, ct);
        if (report == null) return null;
        var items = JsonSerializer.Deserialize<List<EvalResultItemDto>>(report.ItemsJson) ?? new();
        return new EvaluationReportDto(
            report.Id, report.TenantId, report.DatasetName, report.TotalCases,
            report.FaithfulnessAvg, report.AnswerRelevancyAvg, report.ContextPrecisionAvg,
            report.StartedAt, report.CompletedAt, report.Status, items);
    }

    public async Task<List<EvaluationReportDto>> ListReportsAsync(Guid tenantId, int limit = 20, CancellationToken ct = default)
    {
        var reports = await _db.EvalReports
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.StartedAt)
            .Take(limit)
            .ToListAsync(ct);
        return reports.Select(r => new EvaluationReportDto(
            r.Id, r.TenantId, r.DatasetName, r.TotalCases,
            r.FaithfulnessAvg, r.AnswerRelevancyAvg, r.ContextPrecisionAvg,
            r.StartedAt, r.CompletedAt, r.Status, new())).ToList();
    }

    // === 启发式打分（RAGAS 简化版） ===

    /// <summary>
    /// Faithfulness：答案中能在上下文中找到的语句比例。
    /// 简化做法：将答案拆为句子，每句计算与上下文最长公共子串覆盖率。
    /// </summary>
    private static double ScoreFaithfulness(string answer, string context)
    {
        if (string.IsNullOrWhiteSpace(answer)) return 0;
        if (string.IsNullOrWhiteSpace(context)) return 0.5; // 无上下文无法验证

        var sentences = SplitSentences(answer);
        if (sentences.Count == 0) return 0;

        int faithful = 0;
        foreach (var s in sentences)
        {
            // 句子中至少 50% 字符出现在上下文中 → 视为忠实
            var overlap = OverlapRatio(s, context);
            if (overlap >= 0.5) faithful++;
        }
        return Math.Round((double)faithful / sentences.Count, 3);
    }

    /// <summary>
    /// Answer Relevancy：答案与问题的 token 重合度。
    /// 简化做法：Jaccard 相似度。
    /// </summary>
    private static double ScoreAnswerRelevancy(string answer, string question)
    {
        if (string.IsNullOrWhiteSpace(answer) || string.IsNullOrWhiteSpace(question)) return 0;
        var aTokens = Tokenize(answer);
        var qTokens = Tokenize(question);
        var intersection = aTokens.Intersect(qTokens).Count();
        var union = aTokens.Union(qTokens).Count();
        return union == 0 ? 0 : Math.Round((double)intersection / union, 3);
    }

    /// <summary>
    /// Context Precision：检索到的上下文与参考答案的重合度。
    /// 简化做法：参考答案中关键词有多少出现在上下文中。
    /// </summary>
    private static double ScoreContextPrecision(string context, string question, string groundTruth)
    {
        if (string.IsNullOrWhiteSpace(context) || string.IsNullOrWhiteSpace(groundTruth)) return 0;
        var refTokens = Tokenize(groundTruth);
        if (refTokens.Count == 0) return 0;
        int hit = refTokens.Count(t => context.Contains(t, StringComparison.OrdinalIgnoreCase));
        return Math.Round((double)hit / refTokens.Count, 3);
    }

    // === 工具 ===

    private static List<string> SplitSentences(string text)
        => text.Split(new[] { '。', '！', '？', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();

    private static double OverlapRatio(string sentence, string context)
    {
        if (string.IsNullOrEmpty(sentence)) return 0;
        int hit = 0, total = sentence.Length;
        // 简化：滑动窗口 4 字符 n-gram 命中率
        for (int i = 0; i <= sentence.Length - 4; i += 2)
        {
            var gram = sentence.Substring(i, 4);
            if (context.Contains(gram, StringComparison.OrdinalIgnoreCase)) hit++;
        }
        int grams = Math.Max(1, (sentence.Length - 4) / 2 + 1);
        return (double)hit / grams;
    }

    private static HashSet<string> Tokenize(string text)
    {
        // 简单按非字母数字切分，并去长度 ≤ 1 的 token
        var tokens = System.Text.RegularExpressions.Regex.Split(text, @"[^A-Za-z0-9一-龥]+")
            .Where(t => t.Length > 1)
            .Select(t => t.ToLowerInvariant());
        return new HashSet<string>(tokens);
    }

    private static TenantSettingsDto DefaultSettings() => new(
        "你是 AI 客服，请基于上下文准确回答。",
        "您好，请问有什么可以帮您？",
        new[] { "人工", "转人工" },
        null,
        false
    );
}
