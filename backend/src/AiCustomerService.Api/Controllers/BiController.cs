using AiCustomerService.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiCustomerService.Api.Controllers;

/// <summary>
/// BI 报表端点（v0.3.0+）— 供后台 Dashboard 使用
/// </summary>
[ApiController]
[Route("api/v1/bi")]
[Authorize]
public class BiController : ControllerBase
{
    private readonly BiService _bi;
    public BiController(BiService bi) { _bi = bi; }

    [HttpGet("overview")]
    public async Task<ActionResult<DashboardOverviewDto>> Overview(
        [FromQuery] int days = 30, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();
        return Ok(await _bi.GetOverviewAsync(tenantId, days, ct));
    }

    [HttpGet("trend")]
    public async Task<ActionResult<List<TrendPointDto>>> Trend(
        [FromQuery] int days = 7, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();
        return Ok(await _bi.GetConversationTrendAsync(tenantId, days, ct));
    }

    [HttpGet("intention")]
    public async Task<ActionResult<List<DistributionPointDto>>> Intention(CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        return Ok(await _bi.GetIntentionDistributionAsync(tenantId, ct));
    }

    [HttpGet("hot-questions")]
    public async Task<ActionResult<List<HotQuestionDto>>> HotQuestions(
        [FromQuery] int topN = 10,
        [FromQuery] int days = 7,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();
        return Ok(await _bi.GetHotQuestionsAsync(tenantId, topN, days, ct));
    }

    [HttpGet("ai-usage")]
    public async Task<ActionResult<AiUsageSummaryDto>> AiUsage(
        [FromQuery] int days = 30, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();
        return Ok(await _bi.GetAiUsageAsync(tenantId, days, ct));
    }

    private Guid RequireTenantId()
    {
        var v = HttpContext.User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(v)) throw new UnauthorizedAccessException("未登录");
        return Guid.Parse(v);
    }
}
