using System.Text;
using AiCustomerService.Core.Entities;
using AiCustomerService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace AiCustomerService.Infrastructure.AI.RAG;

/// <summary>
/// pgvector 向量存储与余弦检索
/// </summary>
public class PgVectorStore
{
    private readonly AppDbContext _db;

    public PgVectorStore(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddChunksAsync(
        Guid documentId, Guid tenantId,
        List<(TextChunk Chunk, float[] Vector)> items,
        CancellationToken ct = default)
    {
        var entities = items.Select(it => new KnowledgeChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            TenantId = tenantId,
            ChunkIndex = it.Chunk.ChunkIndex,
            Content = it.Chunk.Content,
            ContentLength = it.Chunk.Content.Length,
            Embedding = new Vector(it.Vector),
            Metadata = $"{{\"start\":{it.Chunk.StartIndex},\"end\":{it.Chunk.EndIndex}}}",
            CreatedAt = DateTime.UtcNow
        }).ToList();

        await _db.KnowledgeChunks.AddRangeAsync(entities, ct);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// pgvector 余弦距离检索：使用原生 SQL + &lt;=&gt; 操作符
    /// </summary>
    public async Task<List<KnowledgeChunk>> SimilarSearchAsync(
        Guid tenantId, float[] queryVector, int topK = 5, double minSimilarity = 0.6,
        CancellationToken ct = default)
    {
        var vecLiteral = FormatVectorLiteral(queryVector);

        // pgvector cosine distance operator: <=>  (1 - cosine_similarity)
        var sql = $@"
            SELECT id, tenant_id, document_id, chunk_index, content, content_length,
                   embedding::text, metadata, created_at
            FROM knowledge_chunks
            WHERE tenant_id = {{0}}
              AND embedding IS NOT NULL
            ORDER BY embedding <=> {vecLiteral}::vector
            LIMIT {{1}}";

        var rows = await _db.KnowledgeChunks
            .FromSqlRaw(sql, tenantId, topK * 2)
            .AsNoTracking()
            .ToListAsync(ct);

        return rows
            .Select(c => new
            {
                Chunk = c,
                Distance = ComputeCosineDistance(queryVector, c.Embedding!.ToArray())
            })
            .Where(x => 1.0 - x.Distance >= minSimilarity)
            .OrderBy(x => x.Distance)
            .Take(topK)
            .Select(x => x.Chunk)
            .ToList();
    }

    public async Task DeleteByDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        await _db.KnowledgeChunks
            .Where(c => c.DocumentId == documentId)
            .ExecuteDeleteAsync(ct);
    }

    private static string FormatVectorLiteral(float[] vec)
    {
        var sb = new StringBuilder("[");
        for (int i = 0; i < vec.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(vec[i].ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        sb.Append(']');
        return sb.ToString();
    }

    private static double ComputeCosineDistance(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 1.0;
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }
        if (na == 0 || nb == 0) return 1.0;
        return 1.0 - dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }
}