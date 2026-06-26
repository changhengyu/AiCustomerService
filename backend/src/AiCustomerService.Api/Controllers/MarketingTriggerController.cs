using AiCustomerService.Core.Entities;
using AiCustomerService.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiCustomerService.Api.Controllers;

[ApiController]
[Route("api/v1/marketing/triggers")]
[Authorize]
public class MarketingTriggerController : ControllerBase
{
    private readonly MarketingTriggerService _svc;
    public MarketingTriggerController(MarketingTriggerService svc) { _svc = svc; }

    [HttpGet]
    public async Task<ActionResult<List<MarketingTrigger>>> List(CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        return Ok(await _svc.ListAsync(tenantId, ct));
    }

    public record CreateTriggerRequest(string Name, string EventType, string Conditions, string Actions);

    [HttpPost]
    public async Task<ActionResult<MarketingTrigger>> Create([FromBody] CreateTriggerRequest req, CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        var t = await _svc.CreateAsync(tenantId, req.Name, req.EventType, req.Conditions, req.Actions, ct);
        return Created($"/api/v1/marketing/triggers/{t.Id}", t);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        return await _svc.DeleteAsync(tenantId, id, ct) ? NoContent() : NotFound();
    }

    private Guid RequireTenantId()
    {
        var v = HttpContext.User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(v)) throw new UnauthorizedAccessException("未登录");
        return Guid.Parse(v);
    }
}
