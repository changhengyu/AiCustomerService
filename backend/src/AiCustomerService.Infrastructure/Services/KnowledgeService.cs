using AiCustomerService.Core.DTOs.Knowledge;
using AiCustomerService.Core.Entities;
using AiCustomerService.Core.Exceptions;
using AiCustomerService.Core.Interfaces;
using AiCustomerService.Infrastructure.Data;
using AiCustomerService.Infrastructure.Jobs;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace AiCustomerService.Infrastructure.Services;

public class KnowledgeService : IKnowledgeService
{
    private const string UploadDir = "uploads/knowledge";

    private readonly AppDbContext _db;
    private readonly IBackgroundJobClient _jobs;

    public KnowledgeService(AppDbContext db, IBackgroundJobClient jobs)
    {
        _db = db;
        _jobs = jobs;
    }

    public async Task<Guid> UploadAsync(
        Guid tenantId, Guid userId, Stream stream, string fileName, string title,
        CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        var allowedExts = new[] { "pdf", "docx", "txt", "md", "csv" };
        if (!allowedExts.Contains(ext))
            throw new ValidationException($"不支持的文件类型: {ext}");

        Directory.CreateDirectory(UploadDir);
        var filePath = Path.Combine(UploadDir, $"{Guid.NewGuid()}.{ext}");
        await using (var fs = File.Create(filePath))
            await stream.CopyToAsync(fs, ct);

        var doc = new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = title,
            SourceType = "upload",
            FilePath = filePath,
            FileSize = new FileInfo(filePath).Length,
            Status = "pending",
            UploadedBy = userId,
            CreatedAt = DateTime.UtcNow
        };
        _db.KnowledgeDocuments.Add(doc);
        await _db.SaveChangesAsync(ct);

        var jobId = _jobs.Enqueue<IngestDocumentJob>(
            j => j.ExecuteAsync(doc.Id, filePath, ext));
        doc.JobId = jobId;
        await _db.SaveChangesAsync(ct);

        return doc.Id;
    }

    public async Task<PagedResult<DocumentDto>> ListAsync(
        Guid tenantId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.KnowledgeDocuments.Where(d => d.TenantId == tenantId);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DocumentDto(
                d.Id, d.Title, d.Status, d.ChunkCount, d.FileSize,
                d.CreatedAt, d.ProcessedAt, d.ErrorMessage, d.JobId
            ))
            .ToListAsync(ct);
        return new PagedResult<DocumentDto>(items, total, page, pageSize);
    }

    public async Task DeleteAsync(Guid tenantId, Guid documentId, CancellationToken ct = default)
    {
        var doc = await _db.KnowledgeDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId, ct)
            ?? throw new NotFoundException("文档不存在");

        await _db.KnowledgeChunks
            .Where(c => c.DocumentId == documentId)
            .ExecuteDeleteAsync(ct);

        doc.Status = "deleted";
        await _db.SaveChangesAsync(ct);

        if (!string.IsNullOrEmpty(doc.FilePath) && File.Exists(doc.FilePath))
            try { File.Delete(doc.FilePath); } catch { }
    }

    public async Task<JobStatusDto> GetJobStatusAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = await _db.KnowledgeDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId, ct)
            ?? throw new NotFoundException("文档不存在");
        return new JobStatusDto(doc.Status ?? "unknown", doc.ChunkCount, doc.ChunkCount, doc.ErrorMessage);
    }

    public async Task ReindexAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = await _db.KnowledgeDocuments.FirstOrDefaultAsync(d => d.Id == documentId, ct)
            ?? throw new NotFoundException("文档不存在");
        if (string.IsNullOrEmpty(doc.FilePath))
            throw new ValidationException("文档无文件路径");

        var ext = Path.GetExtension(doc.FilePath).TrimStart('.');
        _jobs.Enqueue<IngestDocumentJob>(j => j.ExecuteAsync(documentId, doc.FilePath, ext));
    }

    public async Task<List<ChunkDto>> GetChunksAsync(
        Guid documentId, int page, int pageSize, CancellationToken ct = default)
    {
        return await _db.KnowledgeChunks
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.ChunkIndex)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ChunkDto(
                c.Id, c.ChunkIndex, c.Content, c.ContentLength, c.Metadata
            ))
            .ToListAsync(ct);
    }
}