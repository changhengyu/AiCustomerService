using AiCustomerService.Infrastructure.Data.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiCustomerService.Infrastructure.Data;

/// <summary>
/// 启动时确保行业 FAQ 种子数据已植入数据库。
/// 幂等：仅在表为空时插入。
/// </summary>
public static class IndustryFaqSeeder
{
    public static async Task SeedAsync(AppDbContext db, ILogger logger, CancellationToken ct = default)
    {
        if (await db.IndustryFaqs.AnyAsync(ct))
        {
            logger.LogDebug("IndustryFaqs 已存在数据，跳过种子");
            return;
        }

        var faqs = IndustryFaqSeed.GetAll();
        db.IndustryFaqs.AddRange(faqs);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("已植入 {Count} 条行业 FAQ 种子数据，涵盖 {Industries} 个行业",
            faqs.Count, faqs.Select(f => f.IndustryCode).Distinct().Count());
    }
}
