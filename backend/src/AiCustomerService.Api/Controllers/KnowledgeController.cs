using AiCustomerService.Core.DTOs.Knowledge;
using AiCustomerService.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiCustomerService.Api.Controllers;

[ApiController]
[Route("api/v1/knowledge")]
[Authorize]
public class KnowledgeController : ControllerBase
{
    private readonly IKnowledgeService _service;
    private readonly ITenantContext _tenantCtx;

    public KnowledgeController(IKnowledgeService service, ITenantContext tenantCtx)
    {
        _service = service;
        _tenantCtx = tenantCtx;
    }

    [HttpGet("documents")]
    public async Task<ActionResult<PagedResult<DocumentDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(_tenantCtx.RequireTenantId(), page, pageSize, ct);
        return Ok(result);
    }

    [HttpPost("documents")]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<Guid>> Upload(
        [FromForm] string title,
        [FromForm] IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "文件不能为空" });

        await using var stream = file.OpenReadStream();
        var docId = await _service.UploadAsync(
            _tenantCtx.RequireTenantId(),
            _tenantCtx.CurrentUserId ?? Guid.Empty,
            stream,
            file.FileName,
            title,
            ct);
        return Ok(new { id = docId });
    }

    [HttpDelete("documents/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(_tenantCtx.RequireTenantId(), id, ct);
        return NoContent();
    }

    [HttpGet("documents/{id:guid}/job")]
    public async Task<ActionResult<JobStatusDto>> GetJob(Guid id, CancellationToken ct)
    {
        var status = await _service.GetJobStatusAsync(id, ct);
        return Ok(status);
    }

    [HttpPost("documents/{id:guid}/reindex")]
    public async Task<IActionResult> Reindex(Guid id, CancellationToken ct)
    {
        await _service.ReindexAsync(id, ct);
        return Accepted();
    }

    [HttpGet("documents/{id:guid}/chunks")]
    public async Task<ActionResult<List<ChunkDto>>> GetChunks(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var chunks = await _service.GetChunksAsync(id, page, pageSize, ct);
        return Ok(chunks);
    }
}