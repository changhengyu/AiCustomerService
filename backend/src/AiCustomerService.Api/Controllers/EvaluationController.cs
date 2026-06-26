using AiCustomerService.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiCustomerService.Api.Controllers;

/// <summary>
/// 评测管理（内部用）— v0.2.0 RAGAS 评估端点
/// </summary>
[ApiController]
[Route("api/v1/eval")]
[Authorize]
public class EvaluationController : ControllerBase
{
    private readonly IEvaluationService _service;
    private readonly ITenantContext _tenantCtx;

    public EvaluationController(IEvaluationService service, ITenantContext tenantCtx)
    {
        _service = service;
        _tenantCtx = tenantCtx;
    }

    /// <summary>运行评测</summary>
    [HttpPost("run")]
    public async Task<ActionResult<EvaluationReportDto>> Run(
        [FromBody] EvaluationRequestDto request, CancellationToken ct)
    {
        var tenantId = _tenantCtx.RequireTenantId();
        var req = request with { TenantId = tenantId };
        var report = await _service.RunAsync(req, ct);
        return Ok(report);
    }

    /// <summary>获取评测报告</summary>
    [HttpGet("reports/{id:guid}")]
    public async Task<ActionResult<EvaluationReportDto>> Get(Guid id, CancellationToken ct)
    {
        var report = await _service.GetReportAsync(id, ct);
        if (report == null) return NotFound();
        return Ok(report);
    }

    /// <summary>列出本租户历史评测报告</summary>
    [HttpGet("reports")]
    public async Task<ActionResult<List<EvaluationReportDto>>> List(
        [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var reports = await _service.ListReportsAsync(_tenantCtx.RequireTenantId(), limit, ct);
        return Ok(reports);
    }
}
