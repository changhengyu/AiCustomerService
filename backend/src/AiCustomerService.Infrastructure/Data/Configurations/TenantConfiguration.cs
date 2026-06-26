using AiCustomerService.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiCustomerService.Infrastructure.Data.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> b)
    {
        b.ToTable("tenants");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        b.Property(x => x.ContactName).HasColumnName("contact_name").HasMaxLength(50);
        b.Property(x => x.ContactPhone).HasColumnName("contact_phone").HasMaxLength(20);
        b.Property(x => x.ContactEmail).HasColumnName("contact_email").HasMaxLength(100);
        b.Property(x => x.Plan).HasColumnName("plan").HasMaxLength(20).HasDefaultValue("free");
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("active");
        b.Property(x => x.MonthlyMessageQuota).HasColumnName("monthly_message_quota").HasDefaultValue(100);
        b.Property(x => x.MonthlyMessageUsed).HasColumnName("monthly_message_used").HasDefaultValue(0);
        b.Property(x => x.Settings).HasColumnName("settings").HasColumnType("jsonb").HasDefaultValue("{}");
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        b.HasIndex(x => x.Status);
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.Username).HasColumnName("username").HasMaxLength(50).IsRequired();
        b.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
        b.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(50);
        b.Property(x => x.Email).HasColumnName("email").HasMaxLength(100);
        b.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(20);
        b.Property(x => x.Role).HasColumnName("role").HasMaxLength(20).HasDefaultValue("agent");
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("active");
        b.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        b.HasIndex(x => new { x.TenantId, x.Username }).IsUnique();
        b.HasOne(x => x.Tenant).WithMany(t => t.Users).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ChannelConfigConfiguration : IEntityTypeConfiguration<ChannelConfig>
{
    public void Configure(EntityTypeBuilder<ChannelConfig> b)
    {
        b.ToTable("channel_configs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.ChannelType).HasColumnName("channel_type").HasMaxLength(30).IsRequired();
        b.Property(x => x.ChannelName).HasColumnName("channel_name").HasMaxLength(50).IsRequired();
        b.Property(x => x.AppId).HasColumnName("app_id").HasMaxLength(100);
        b.Property(x => x.AppSecretEncrypted).HasColumnName("app_secret_encrypted");
        b.Property(x => x.WebhookToken).HasColumnName("webhook_token").HasMaxLength(100);
        b.Property(x => x.EncodingAesKey).HasColumnName("encoding_aes_key").HasMaxLength(100);
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("active");
        b.Property(x => x.ExtraConfig).HasColumnName("extra_config").HasColumnType("jsonb").HasDefaultValue("{}");
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        b.HasIndex(x => x.TenantId);
        b.HasIndex(x => new { x.TenantId, x.ChannelType, x.AppId }).IsUnique();
        b.HasOne(x => x.Tenant).WithMany(t => t.ChannelConfigs).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}
