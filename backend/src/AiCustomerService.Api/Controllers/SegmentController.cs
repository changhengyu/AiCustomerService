using AiCustomerService.Core.Entities;
using AiCustomerService.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiCustomerService.Api.Controllers;

[ApiController]
[Route("api/v1/segments")]
[Authorize]
public class SegmentController : ControllerBase
{
    private readonly SegmentService _svc;
    public SegmentController(SegmentService svc) { _svc = svc; }

    [HttpGet]
    public async Task<ActionResult<List<CustomerSegment>>> List(CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        return Ok(await _svc.ListAsync(tenantId, ct));
    }

    public record CreateSegmentRequest(string Name, string? Description, string Rules);

    [HttpPost]
    public async Task<ActionResult<CustomerSegment>> Create([FromBody] CreateSegmentRequest req, CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        var s = await _svc.CreateAsync(tenantId, req.Name, req.Description, req.Rules, ct);
        return Created($"/api/v1/segments/{s.Id}", s);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        return await _svc.DeleteAsync(tenantId, id, ct) ? NoContent() : NotFound();
    }

    [HttpPost("{id}/evaluate")]
    public async Task<IActionResult> Evaluate(Guid id, CancellationToken ct)
    {
        var count = await _svc.RecomputeAsync(id, ct);
        return Ok(new { member_count = count });
    }

    [HttpGet("{id}/members")]
    public async Task<IActionResult> Members(Guid id, [FromQuery] int limit = 200, CancellationToken ct = default)
    {
        var members = await _svc.GetMembersAsync(id, limit, ct);
        return Ok(members.Select(c => new { c.Id, c.Nickname, c.Tags, c.IntentionLevel, c.Region, c.LifecycleStage }));
    }

    private Guid RequireTenantId()
    {
        var v = HttpContext.User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(v)) throw new UnauthorizedAccessException("未登录");
        return Guid.Parse(v);
    }
}
