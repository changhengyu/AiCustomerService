using AiCustomerService.Core.Entities;
using AiCustomerService.Core.Interfaces;
using AiCustomerService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AiCustomerService.Infrastructure.AI.RAG;

public record RetrievalHit(KnowledgeChunk Chunk, double Score, string MatchType);

public class HybridRetriever
{
    private readonly PgVectorStore _vector;
    private readonly IEmbeddingService _embedding;
    private readonly AppDbContext _db;

    // 关键词权重（pg_trgm 全文搜索的相似度）
    private const double KeywordWeight = 0.4;
    private const double VectorWeight = 0.6;

    public HybridRetriever(PgVectorStore vector, IEmbeddingService embedding, AppDbContext db)
    {
        _vector = vector;
        _embedding = embedding;
        _db = db;
    }

    public async Task<List<RetrievalHit>> RetrieveAsync(
        Guid tenantId, string query, int topK = 5, double minScore = 0.5,
        CancellationToken ct = default)
    {
        // 1) 向量检索
        var queryVec = await _embedding.EmbedAsync(query, ct);
        var vectorHits = await _vector.SimilarSearchAsync(tenantId, queryVec, topK * 2, 0.0, ct);

        // 2) 关键词检索（PostgreSQL ILIKE 简单实现，后续可升级到 tsvector + pg_trgm）
        var keywordHits = await KeywordSearchAsync(tenantId, query, topK * 2, ct);

        // 3) 混合排序
        var merged = Merge(vectorHits, keywordHits, topK, minScore);
        return merged;
    }

    private async Task<List<KnowledgeChunk>> KeywordSearchAsync(
        Guid tenantId, string query, int limit, CancellationToken ct)
    {
        var keywords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 2)
            .Take(8)
            .ToList();
        if (keywords.Count == 0) return new List<KnowledgeChunk>();

        var q = _db.KnowledgeChunks.Where(c => c.TenantId == tenantId);
        foreach (var k in keywords)
            q = q.Where(c => EF.Functions.ILike(c.Content, $"%{k}%"));

        return await q.OrderByDescending(c => c.CreatedAt).Take(limit).ToListAsync(ct);
    }

    private List<RetrievalHit> Merge(
        List<KnowledgeChunk> vectorHits,
        List<KnowledgeChunk> keywordHits,
        int topK, double minScore)
    {
        var dict = new Dictionary<Guid, (KnowledgeChunk chunk, double score, string match)>();

        foreach (var v in vectorHits)
        {
            // 转换为分数（用排名近似，越靠前越高）
            var idx = vectorHits.IndexOf(v);
            var score = (1.0 - idx / (double)vectorHits.Count) * VectorWeight;
            if (!dict.ContainsKey(v.Id) || dict[v.Id].score < score)
                dict[v.Id] = (v, score, "vector");
        }

        foreach (var k in keywordHits)
        {
            var idx = keywordHits.IndexOf(k);
            var score = (1.0 - idx / (double)Math.Max(1, keywordHits.Count)) * KeywordWeight;
            if (dict.TryGetValue(k.Id, out var existing))
                dict[k.Id] = (existing.chunk, existing.score + score, "hybrid");
            else
                dict[k.Id] = (k, score, "keyword");
        }

        return dict.Values
            .Where(x => x.score >= minScore)
            .OrderByDescending(x => x.score)
            .Take(topK)
            .Select(x => new RetrievalHit(x.chunk, x.score, x.match))
            .ToList();
    }
}