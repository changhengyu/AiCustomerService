using System.ComponentModel.DataAnnotations;

namespace AiCustomerService.Core.Entities;

/// <summary>客户备注</summary>
public class CustomerNote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? AuthorUserId { get; set; }

    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
