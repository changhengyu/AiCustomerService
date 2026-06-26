using AiCustomerService.Core.DTOs.Knowledge;
using AiCustomerService.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiCustomerService.Api.Controllers;

[ApiController]
[Route("api/v1/customers")]
[Authorize]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _service;
    private readonly ITenantContext _tenantCtx;

    public CustomerController(ICustomerService service, ITenantContext tenantCtx)
    {
        _service = service;
        _tenantCtx = tenantCtx;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerListItemDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? intentionLevel = null,
        [FromQuery] string? keyword = null,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(_tenantCtx.RequireTenantId(), page, pageSize,
            intentionLevel, keyword, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDetailDto>> Get(Guid id, CancellationToken ct)
    {
        var detail = await _service.GetDetailAsync(_tenantCtx.RequireTenantId(), id, ct);
        if (detail == null) return NotFound();
        return Ok(detail);
    }

    [HttpPut("{id:guid}/tags")]
    public async Task<IActionResult> UpdateTags(
        Guid id, [FromBody] UpdateTagsRequest request, CancellationToken ct)
    {
        await _service.UpdateTagsAsync(_tenantCtx.RequireTenantId(), id, request.Tags ?? Array.Empty<string>(), ct);
        return NoContent();
    }
}

public record UpdateTagsRequest(string[] Tags);