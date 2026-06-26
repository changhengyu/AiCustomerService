using AiCustomerService.Core.Entities;
using AiCustomerService.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiCustomerService.Api.Controllers;

/// <summary>开放 API 管理（v0.3.0+）— API Key + Webhook</summary>
[ApiController]
[Route("api/v1/open")]
[Authorize]
public class OpenApiController : ControllerBase
{
    private readonly OpenApiService _svc;
    public OpenApiController(OpenApiService svc) { _svc = svc; }

    // ===== API Key =====

    public record CreateApiKeyRequest(string Name, string Scopes = "read", DateTime? ExpiresAt = null);

    [HttpPost("keys")]
    public async Task<IActionResult> CreateKey([FromBody] CreateApiKeyRequest req, CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        var (key, plaintext) = await _svc.CreateApiKeyAsync(tenantId, req.Name, req.Scopes, req.ExpiresAt, ct);
        return Created($"/api/v1/open/keys/{key.Id}", new
        {
            key.Id, key.Prefix, key.Name, key.Scopes, key.ExpiresAt, key.CreatedAt,
            PlainTextKey = plaintext  // 仅返回一次！
        });
    }

    [HttpGet("keys")]
    public async Task<ActionResult<List<ApiKey>>> ListKeys(CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        return Ok(await _svc.ListApiKeysAsync(tenantId, ct));
    }

    [HttpDelete("keys/{id}")]
    public async Task<IActionResult> RevokeKey(Guid id, CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        return await _svc.RevokeApiKeyAsync(tenantId, id, ct) ? NoContent() : NotFound();
    }

    // ===== Webhook =====

    public record CreateWebhookRequest(string Name, string Url, string Events);

    [HttpPost("webhooks")]
    public async Task<ActionResult<WebhookConfig>> CreateWebhook([FromBody] CreateWebhookRequest req, CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        var w = await _svc.CreateWebhookAsync(tenantId, req.Name, req.Url, req.Events, ct);
        return Created($"/api/v1/open/webhooks/{w.Id}", w);
    }

    [HttpGet("webhooks")]
    public async Task<ActionResult<List<WebhookConfig>>> ListWebhooks(CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        return Ok(await _svc.ListWebhooksAsync(tenantId, ct));
    }

    [HttpDelete("webhooks/{id}")]
    public async Task<IActionResult> DeleteWebhook(Guid id, CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        return await _svc.DeleteWebhookAsync(tenantId, id, ct) ? NoContent() : NotFound();
    }

    // ===== 投递触发（管理用）=====

    [HttpPost("webhooks/dispatch")]
    public async Task<IActionResult> Dispatch(CancellationToken ct)
    {
        var n = await _svc.DispatchPendingAsync(50, ct);
        return Ok(new { Delivered = n });
    }

    private Guid RequireTenantId()
    {
        var v = HttpContext.User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(v)) throw new UnauthorizedAccessException("未登录");
        return Guid.Parse(v);
    }
}
