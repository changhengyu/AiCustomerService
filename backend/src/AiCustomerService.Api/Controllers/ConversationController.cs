using AiCustomerService.Core.DTOs.Conversation;
using AiCustomerService.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiCustomerService.Api.Controllers;

[ApiController]
[Route("api/v1/conversations")]
[Authorize]
public class ConversationController : ControllerBase
{
    private readonly IConversationService _service;
    private readonly ITenantContext _tenantCtx;

    public ConversationController(IConversationService service, ITenantContext tenantCtx)
    {
        _service = service;
        _tenantCtx = tenantCtx;
    }

    [HttpGet]
    public async Task<ActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(_tenantCtx.RequireTenantId(), page, pageSize, status, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConversationDetailDto>> Get(Guid id, CancellationToken ct)
    {
        var detail = await _service.GetDetailAsync(id, ct);
        if (detail == null) return NotFound();
        return Ok(detail);
    }

    [HttpPost("{id:guid}/messages/agent")]
    public async Task<ActionResult<SendMessageResponse>> SendAgent(
        Guid id, [FromBody] AgentSendMessageRequest request, CancellationToken ct)
    {
        var resp = await _service.SendAgentMessageAsync(id, request.Content, ct);
        return Ok(resp);
    }

    [HttpPost("{id:guid}/handoff")]
    public async Task<IActionResult> Handoff(Guid id, [FromBody] HandoffRequest? request, CancellationToken ct)
    {
        await _service.HandoffToHumanAsync(id, request?.AssignedTo, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
    {
        await _service.CloseConversationAsync(id, ct);
        return NoContent();
    }
}