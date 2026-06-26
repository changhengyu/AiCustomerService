namespace AiCustomerService.Core.Entities;

public class KnowledgeDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SourceType { get; set; } = "upload";
    public string? SourceUrl { get; set; }
    public string? FilePath { get; set; }
    public long FileSize { get; set; }
    public string? FileHash { get; set; }
    public string Status { get; set; } = "pending";
    public int ChunkCount { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? UploadedBy { get; set; }
    public string? JobId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    public Tenant? Tenant { get; set; }
    public User? Uploader { get; set; }
    public ICollection<KnowledgeChunk> Chunks { get; set; } = new List<KnowledgeChunk>();
}
