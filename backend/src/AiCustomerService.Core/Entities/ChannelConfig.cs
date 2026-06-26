namespace AiCustomerService.Core.Entities;

public class ChannelConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string ChannelType { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public string? AppId { get; set; }
    public string? AppSecretEncrypted { get; set; }
    public string? WebhookToken { get; set; }
    public string? EncodingAesKey { get; set; }
    public string Status { get; set; } = "active";
    public string ExtraConfig { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Tenant? Tenant { get; set; }
}
