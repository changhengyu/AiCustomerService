using AiCustomerService.Core.Entities;
using AiCustomerService.Core.Interfaces;
using AiCustomerService.Infrastructure.AI.RAG;
using AiCustomerService.Infrastructure.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiCustomerService.Infrastructure.Jobs;

/// <summary>
/// 文档摄取后台任务：清洗 -> 切分 -> Embedding -> 入库
/// </summary>
public class IngestDocumentJob
{
    private const string StatusPending = "pending";
    private const string StatusProcessing = "processing";
    private const string StatusReady = "ready";
    private const string StatusFailed = "failed";

    private readonly AppDbContext _db;
    private readonly DocumentLoader _loader;
    private readonly TextCleaner _cleaner;
    private readonly TextSplitter _splitter;
    private readonly EmbeddingBatcher _batcher;
    private readonly PgVectorStore _vectorStore;
    private readonly ILogger<IngestDocumentJob> _logger;

    public IngestDocumentJob(
        AppDbContext db,
        DocumentLoader loader,
        TextCleaner cleaner,
        TextSplitter splitter,
        EmbeddingBatcher batcher,
        PgVectorStore vectorStore,
        ILogger<IngestDocumentJob> logger)
    {
        _db = db;
        _loader = loader;
        _cleaner = cleaner;
        _splitter = splitter;
        _batcher = batcher;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public async Task ExecuteAsync(Guid documentId, string filePath, string fileType)
    {
        var doc = await _db.KnowledgeDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId);
        if (doc == null)
        {
            _logger.LogWarning("文档不存在: {Id}", documentId);
            return;
        }

        doc.Status = StatusProcessing;
        await _db.SaveChangesAsync();

        try
        {
            using var stream = File.OpenRead(filePath);
            var loaded = await _loader.LoadAsync(stream, doc.Title, fileType);
            var cleaned = _cleaner.Clean(loaded.Content);

            var chunks = _splitter.Split(cleaned, chunkSize: 500, overlap: 80);
            if (chunks.Count == 0)
                throw new InvalidOperationException("文档切分结果为空");

            var embedded = await _batcher.EmbedChunksAsync(chunks);
            await _vectorStore.DeleteByDocumentAsync(documentId);
            await _vectorStore.AddChunksAsync(documentId, doc.TenantId, embedded);

            doc.Status = StatusReady;
            doc.ChunkCount = chunks.Count;
            doc.ProcessedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "文档 {Title} 摄取完成: {Chunks} 个分块", doc.Title, chunks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文档摄取失败: {Id}", documentId);
            doc.Status = StatusFailed;
            doc.ErrorMessage = ex.Message;
            await _db.SaveChangesAsync();
            throw;
        }
    }
}