using AiCustomerService.Core.DTOs.AI;
using AiCustomerService.Core.DTOs.Conversation;
using AiCustomerService.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiCustomerService.Api.Controllers;

[ApiController]
[Route("api/v1/chat")]
public class ChatController : ControllerBase
{
    private readonly IConversationService _service;
    private readonly ITenantContext _tenantCtx;

    public ChatController(IConversationService service, ITenantContext tenantCtx)
    {
        _service = service;
        _tenantCtx = tenantCtx;
    }

    /// <summary>
    /// 内部/测试用：客户端直接发起对话（无需微信回调）
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<SendMessageResponse>> Send(
        [FromBody] InternalChatRequest request, CancellationToken ct)
    {
        var resp = await _service.HandleUserMessageAsync(
            request.TenantId,
            request.CustomerId,
            request.Content,
            request.ConversationId,
            ct);
        return Ok(resp);
    }
}

public record InternalChatRequest(
    Guid TenantId,
    Guid CustomerId,
    string Content,
    Guid? ConversationId = null
);