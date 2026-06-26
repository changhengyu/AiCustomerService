using AiCustomerService.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiCustomerService.Api.Controllers;

[ApiController]
[Route("api/v1/industry-faqs")]
[Authorize]
public class IndustryFaqController : ControllerBase
{
    private readonly IIndustryFaqService _service;
    private readonly ITenantContext _tenantCtx;

    public IndustryFaqController(IIndustryFaqService service, ITenantContext tenantCtx)
    {
        _service = service;
        _tenantCtx = tenantCtx;
    }

    /// <summary>列出当前租户行业下的全部 FAQ</summary>
    [HttpGet]
    public async Task<ActionResult<List<IndustryFaqDto>>> List(CancellationToken ct)
    {
        var tenantId = _tenantCtx.RequireTenantId();
        // 直接从 tenant 获取 industry_code（需要在 TenantContext 加 getter 或直接查 db）
        // 简化：先取所有行业
        var industries = await _service.ListIndustriesAsync(ct);
        var code = industries.FirstOrDefault() ?? "general";
        return Ok(await _service.ListByIndustryAsync(code, ct));
    }

    /// <summary>列出所有行业代码</summary>
    [HttpGet("industries")]
    [AllowAnonymous]
    public async Task<ActionResult<List<string>>> Industries(CancellationToken ct)
        => Ok(await _service.ListIndustriesAsync(ct));

    /// <summary>按行业代码 + 查询字符串检索 FAQ</summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<IndustryFaqDto>>> Search(
        [FromQuery] string industryCode,
        [FromQuery] string q,
        [FromQuery] int topK = 3,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(industryCode) || string.IsNullOrEmpty(q))
            return BadRequest(new { message = "industryCode 与 q 必填" });
        return Ok(await _service.SearchAsync(industryCode, q, topK, ct));
    }
}
