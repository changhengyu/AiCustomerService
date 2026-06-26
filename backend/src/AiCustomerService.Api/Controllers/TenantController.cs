using AiCustomerService.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiCustomerService.Api.Controllers;

[ApiController]
[Route("api/v1/tenant")]
[Authorize]
public class TenantController : ControllerBase
{
    private readonly ITenantService _service;
    private readonly ITenantContext _tenantCtx;

    public TenantController(ITenantService service, ITenantContext tenantCtx)
    {
        _service = service;
        _tenantCtx = tenantCtx;
    }

    [HttpGet]
    public async Task<IActionResult> GetCurrent(CancellationToken ct)
    {
        var tenant = await _service.GetCurrentAsync(_tenantCtx.RequireTenantId(), ct);
        if (tenant == null) return NotFound();
        return Ok(new
        {
            tenant.Id,
            tenant.Name,
            tenant.Plan,
            tenant.Status,
            tenant.MonthlyMessageQuota,
            tenant.MonthlyMessageUsed,
            tenant.ContactEmail,
            tenant.ContactPhone
        });
    }

    [HttpGet("settings")]
    public async Task<ActionResult<TenantSettingsDto>> GetSettings(CancellationToken ct)
        => Ok(await _service.GetSettingsAsync(_tenantCtx.RequireTenantId(), ct));

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] TenantSettingsDto settings, CancellationToken ct)
    {
        await _service.UpdateSettingsAsync(_tenantCtx.RequireTenantId(), settings, ct);
        return NoContent();
    }
}