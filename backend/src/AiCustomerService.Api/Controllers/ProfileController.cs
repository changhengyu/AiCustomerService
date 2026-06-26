using AiCustomerService.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiCustomerService.Api.Controllers;

[ApiController]
[Route("api/v1/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly ProfileService _svc;
    public ProfileController(ProfileService svc) { _svc = svc; }

    [HttpGet("{customerId}")]
    public async Task<ActionResult<CustomerProfileDto>> Get(Guid customerId, CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        return Ok(await _svc.GetProfileAsync(tenantId, customerId, ct));
    }

    [HttpPatch("{customerId}")]
    public async Task<IActionResult> Update(Guid customerId, [FromBody] UpdateProfileRequest req, CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        await _svc.UpdateProfileAsync(tenantId, customerId, req, ct);
        return NoContent();
    }

    public record AddNoteRequest(string Content);

    [HttpPost("{customerId}/notes")]
    public async Task<ActionResult<CustomerNoteDto>> AddNote(
        Guid customerId, [FromBody] AddNoteRequest req, CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        var userId = HttpContext.User.FindFirst("user_id")?.Value;
        var n = await _svc.AddNoteAsync(tenantId, customerId, req.Content,
            userId != null ? Guid.Parse(userId) : null, ct);
        return Created($"/api/v1/profile/{customerId}/notes/{n.Id}", n);
    }

    private Guid RequireTenantId()
    {
        var v = HttpContext.User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(v)) throw new UnauthorizedAccessException("未登录");
        return Guid.Parse(v);
    }
}
