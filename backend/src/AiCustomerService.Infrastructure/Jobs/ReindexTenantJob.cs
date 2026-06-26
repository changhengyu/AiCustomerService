using AiCustomerService.Infrastructure.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiCustomerService.Infrastructure.Jobs;

/// <summary>
/// 重建某租户所有文档索引
/// </summary>
public class ReindexTenantJob
{
    private const string StatusActive = "ready";

    private readonly AppDbContext _db;
    private readonly IngestDocumentJob _ingest;
    private readonly ILogger<ReindexTenantJob> _logger;

    public ReindexTenantJob(AppDbContext db, IngestDocumentJob ingest, ILogger<ReindexTenantJob> logger)
    {
        _db = db;
        _ingest = ingest;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 1)]
    public async Task ExecuteAsync(Guid tenantId)
    {
        var docs = await _db.KnowledgeDocuments
            .Where(d => d.TenantId == tenantId && d.Status != "deleted")
            .ToListAsync();

        _logger.LogInformation("租户 {TenantId} 共 {Count} 个文档待重建", tenantId, docs.Count);
        foreach (var doc in docs)
        {
            try
            {
                await _ingest.ExecuteAsync(doc.Id, doc.FilePath ?? "", "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重建失败: {Title}", doc.Title);
            }
        }
    }
}