using System.ComponentModel.DataAnnotations;

namespace AiCustomerService.Core.Entities;

/// <summary>API Key — 租户对外调用开放 API 的凭证</summary>
public class ApiKey
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    /// <summary>对外展示前缀（如 ak_live_xxxxxxxx）</summary>
    [MaxLength(64)]
    public string Prefix { get; set; } = string.Empty;

    /// <summary>SHA-256 哈希后的完整 Key（绝不返回明文）</summary>
    [MaxLength(128)]
    public string HashedKey { get; set; } = string.Empty;

    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    /// <summary>逗号分隔的权限范围：read,write,chat,knowledge,...</summary>
    [MaxLength(256)]
    public string Scopes { get; set; } = "read";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool Revoked { get; set; } = false;

    public Tenant? Tenant { get; set; }
}
