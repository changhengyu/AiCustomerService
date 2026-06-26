using AiCustomerService.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiCustomerService.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("customers");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.ExternalId).HasColumnName("external_id").HasMaxLength(100).IsRequired();
        b.Property(x => x.ChannelType).HasColumnName("channel_type").HasMaxLength(30).IsRequired();
        b.Property(x => x.Nickname).HasColumnName("nickname").HasMaxLength(100);
        b.Property(x => x.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(500);
        b.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(20);
        b.Property(x => x.Gender).HasColumnName("gender").HasMaxLength(10);
        b.Property(x => x.Region).HasColumnName("region").HasMaxLength(100);
        b.Property(x => x.Tags).HasColumnName("tags").HasColumnType("text[]").HasDefaultValue(Array.Empty<string>());
        b.Property(x => x.IntentionScore).HasColumnName("intention_score").HasDefaultValue(0);
        b.Property(x => x.IntentionLevel).HasColumnName("intention_level").HasMaxLength(20).HasDefaultValue("cold");
        b.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb").HasDefaultValue("{}");
        b.Property(x => x.FirstSeenAt).HasColumnName("first_seen_at").HasDefaultValueSql("NOW()");
        b.Property(x => x.LastSeenAt).HasColumnName("last_seen_at").HasDefaultValueSql("NOW()");
        b.HasIndex(x => new { x.TenantId, x.ChannelType, x.ExternalId }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.IntentionLevel });
        b.HasIndex(x => new { x.TenantId, x.LastSeenAt });
        b.HasOne(x => x.Tenant).WithMany(t => t.Customers).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> b)
    {
        b.ToTable("conversations");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.CustomerId).HasColumnName("customer_id");
        b.Property(x => x.ChannelType).HasColumnName("channel_type").HasMaxLength(30).IsRequired();
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("active");
        b.Property(x => x.AssignedTo).HasColumnName("assigned_to");
        b.Property(x => x.MessageCount).HasColumnName("message_count").HasDefaultValue(0);
        b.Property(x => x.LastMessageAt).HasColumnName("last_message_at").HasDefaultValueSql("NOW()");
        b.Property(x => x.Summary).HasColumnName("summary");
        b.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb").HasDefaultValue("{}");
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        b.Property(x => x.ClosedAt).HasColumnName("closed_at");
        b.HasIndex(x => x.TenantId);
        b.HasIndex(x => x.CustomerId);
        b.HasIndex(x => new { x.TenantId, x.Status, x.LastMessageAt });
        b.HasOne(x => x.Tenant).WithMany(t => t.Conversations).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Customer).WithMany(c => c.Conversations).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.AssignedUser).WithMany().HasForeignKey(x => x.AssignedTo).OnDelete(DeleteBehavior.SetNull);
    }
}

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> b)
    {
        b.ToTable("messages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ConversationId).HasColumnName("conversation_id");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.Role).HasColumnName("role").HasMaxLength(20).IsRequired();
        b.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(20).HasDefaultValue("text");
        b.Property(x => x.Content).HasColumnName("content").IsRequired();
        b.Property(x => x.RawPayload).HasColumnName("raw_payload").HasColumnType("jsonb");
        b.Property(x => x.TokensUsed).HasColumnName("tokens_used").HasDefaultValue(0);
        b.Property(x => x.RetrievalChunks).HasColumnName("retrieval_chunks").HasColumnType("jsonb");
        b.Property(x => x.LatencyMs).HasColumnName("latency_ms").HasDefaultValue(0);
        b.Property(x => x.ErrorMessage).HasColumnName("error_message");
        b.Property(x => x.UserRating).HasColumnName("user_rating");
        b.Property(x => x.UserFeedback).HasColumnName("user_feedback");
        b.Property(x => x.FeedbackAt).HasColumnName("feedback_at");
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        b.HasIndex(x => new { x.ConversationId, x.CreatedAt });
        b.HasIndex(x => new { x.TenantId, x.CreatedAt });
        b.HasOne(x => x.Conversation).WithMany(c => c.Messages).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
    }
}
