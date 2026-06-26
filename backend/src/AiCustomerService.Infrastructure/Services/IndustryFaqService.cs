using AiCustomerService.Core.Entities;
using AiCustomerService.Core.Interfaces;
using AiCustomerService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AiCustomerService.Infrastructure.Services;

public class IndustryFaqService : IIndustryFaqService
{
    private readonly AppDbContext _db;
    public IndustryFaqService(AppDbContext db) { _db = db; }

    public async Task<List<IndustryFaqDto>> ListByIndustryAsync(string industryCode, CancellationToken ct = default)
    {
        return await _db.IndustryFaqs
            .Where(f => f.IndustryCode == industryCode)
            .OrderBy(f => f.SortOrder)
            .Select(f => new IndustryFaqDto(f.Id, f.IndustryCode, f.Question, f.Answer, f.Keywords))
            .ToListAsync(ct);
    }

    public async Task<List<string>> ListIndustriesAsync(CancellationToken ct = default)
    {
        return await _db.IndustryFaqs
            .Select(f => f.IndustryCode)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(ct);
    }

    public async Task<List<IndustryFaqDto>> SearchAsync(string industryCode, string query, int topK = 3, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<IndustryFaqDto>();

        var keywords = ExtractKeywords(query);
        var all = await ListByIndustryAsync(industryCode, ct);
        if (all.Count == 0) return new List<IndustryFaqDto>();

        // 简单关键词打分：问题匹配 + 关键词列表匹配
        var scored = all.Select(f => new
        {
            Faq = f,
            Score = Score(f, query, keywords)
        })
        .Where(x => x.Score > 0)
        .OrderByDescending(x => x.Score)
        .Take(topK)
        .Select(x => x.Faq)
        .ToList();

        return scored;
    }

    private static int Score(IndustryFaqDto faq, string query, HashSet<string> qKeywords)
    {
        int score = 0;
        var q = query.ToLowerInvariant();
        // 完全包含问题
        if (q.Contains(faq.Question.ToLowerInvariant()) || faq.Question.ToLowerInvariant().Contains(q))
            score += 5;
        // 关键词命中
        foreach (var kw in faq.Keywords)
        {
            if (q.Contains(kw, StringComparison.OrdinalIgnoreCase))
                score += 3;
            if (qKeywords.Contains(kw.ToLowerInvariant()))
                score += 2;
        }
        return score;
    }

    private static HashSet<string> ExtractKeywords(string text)
    {
        // 简单分词：按空格和标点切分 + 长度 > 1
        var words = text.Split(new[] { ' ', '　', ',', '，', '。', '?', '？', '!', '！', ';', '；', ':', '：', '\n', '\r', '\t' },
            StringSplitOptions.RemoveEmptyEntries);
        return new HashSet<string>(words.Where(w => w.Length > 1).Select(w => w.ToLowerInvariant()));
    }
}
