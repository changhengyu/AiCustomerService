namespace AiCustomerService.Core.Entities;

/// <summary>
/// 行业冷启动 FAQ 模板（tenant_id 为空表示全局模板）。
/// 新租户注册时根据 IndustryCode 自动植入或自动加入 RAG 检索范围。
/// </summary>
public class IndustryFaq
{
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>行业代码：general/ecommerce/education/saas/finance/medical</summary>
    public string IndustryCode { get; set; } = "general";
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    /// <summary>关键词（用于关键词检索加权）</summary>
    public string[] Keywords { get; set; } = Array.Empty<string>();
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
