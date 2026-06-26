using AiCustomerService.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiCustomerService.Infrastructure.Data.Configurations;

public class KnowledgeDocumentConfiguration : IEntityTypeConfiguration<KnowledgeDocument>
{
    public void Configure(EntityTypeBuilder<KnowledgeDocument> b)
    {
        b.ToTable("knowledge_documents");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        b.Property(x => x.SourceType).HasColumnName("source_type").HasMaxLength(20).HasDefaultValue("upload");
        b.Property(x => x.SourceUrl).HasColumnName("source_url").HasMaxLength(500);
        b.Property(x => x.FilePath).HasColumnName("file_path").HasMaxLength(500);
        b.Property(x => x.FileSize).HasColumnName("file_size");
        b.Property(x => x.FileHash).HasColumnName("file_hash").HasMaxLength(64);
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("pending");
        b.Property(x => x.ChunkCount).HasColumnName("chunk_count").HasDefaultValue(0);
        b.Property(x => x.ErrorMessage).HasColumnName("error_message");
        b.Property(x => x.UploadedBy).HasColumnName("uploaded_by");
        b.Property(x => x.JobId).HasColumnName("job_id").HasMaxLength(100);
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        b.Property(x => x.ProcessedAt).HasColumnName("processed_at");
        b.HasIndex(x => x.TenantId);
        b.HasIndex(x => new { x.TenantId, x.Status });
        b.HasIndex(x => new { x.TenantId, x.FileHash });
        b.HasOne(x => x.Tenant).WithMany(t => t.Documents).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Uploader).WithMany().HasForeignKey(x => x.UploadedBy).OnDelete(DeleteBehavior.SetNull);
    }
}

public class KnowledgeChunkConfiguration : IEntityTypeConfiguration<KnowledgeChunk>
{
    public void Configure(EntityTypeBuilder<KnowledgeChunk> b)
    {
        b.ToTable("knowledge_chunks");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.DocumentId).HasColumnName("document_id");
        b.Property(x => x.ChunkIndex).HasColumnName("chunk_index");
        b.Property(x => x.Content).HasColumnName("content").IsRequired();
        b.Property(x => x.ContentLength).HasColumnName("content_length");
        b.Property(x => x.Embedding).HasColumnName("embedding").HasColumnType("vector(1024)");
        b.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb").HasDefaultValue("{}");
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        b.HasIndex(x => x.TenantId);
        b.HasIndex(x => x.DocumentId);
        b.HasIndex(x => x.Metadata).HasMethod("gin");
        b.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Document).WithMany(d => d.Chunks).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> b)
    {
        b.ToTable("subscriptions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.Plan).HasColumnName("plan").HasMaxLength(20).IsRequired();
        b.Property(x => x.StartDate).HasColumnName("start_date");
        b.Property(x => x.EndDate).HasColumnName("end_date");
        b.Property(x => x.AmountCents).HasColumnName("amount_cents");
        b.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasMaxLength(20);
        b.Property(x => x.PaymentId).HasColumnName("payment_id").HasMaxLength(100);
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("pending");
        b.Property(x => x.InvoiceUrl).HasColumnName("invoice_url").HasMaxLength(500);
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        b.Property(x => x.PaidAt).HasColumnName("paid_at");
        b.HasIndex(x => new { x.TenantId, x.Status });
        b.HasOne(x => x.Tenant).WithMany(t => t.Subscriptions).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class IntentionRuleConfiguration : IEntityTypeConfiguration<IntentionRule>
{
    public void Configure(EntityTypeBuilder<IntentionRule> b)
    {
        b.ToTable("intention_rules");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.RuleName).HasColumnName("rule_name").HasMaxLength(100).IsRequired();
        b.Property(x => x.Keywords).HasColumnName("keywords").HasColumnType("text[]").IsRequired();
        b.Property(x => x.ScoreDelta).HasColumnName("score_delta");
        b.Property(x => x.TargetLevel).HasColumnName("target_level").HasMaxLength(20);
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        b.HasIndex(x => x.TenantId);
    }
}

public class AiUsageLogConfiguration : IEntityTypeConfiguration<AiUsageLog>
{
    public void Configure(EntityTypeBuilder<AiUsageLog> b)
    {
        b.ToTable("ai_usage_logs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.ConversationId).HasColumnName("conversation_id");
        b.Property(x => x.Model).HasColumnName("model").HasMaxLength(50).IsRequired();
        b.Property(x => x.PromptTokens).HasColumnName("prompt_tokens").HasDefaultValue(0);
        b.Property(x => x.CompletionTokens).HasColumnName("completion_tokens").HasDefaultValue(0);
        b.Property(x => x.TotalTokens).HasColumnName("total_tokens").HasDefaultValue(0);
        b.Property(x => x.CostCents).HasColumnName("cost_cents").HasDefaultValue(0);
        b.Property(x => x.LatencyMs).HasColumnName("latency_ms").HasDefaultValue(0);
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        b.HasIndex(x => new { x.TenantId, x.CreatedAt });
    }
}
