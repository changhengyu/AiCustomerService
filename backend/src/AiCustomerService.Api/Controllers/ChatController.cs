using AiCustomerService.Core.DTOs.AI;
using AiCustomerService.Core.DTOs.Conversation;
using AiCustomerService.Core.Interfaces;
using AiCustomerService.Infrastructure.AI.Stt;
using AiCustomerService.Infrastructure.Observability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AiCustomerService.Api.Controllers;

[ApiController]
[Route("api/v1/chat")]
public class ChatController : ControllerBase
{
    private readonly IConversationService _service;
    private readonly ITenantContext _tenantCtx;
    private readonly IAiSttProvider _stt;
    private readonly Infrastructure.Services.OpenApiService _webhooks;
    private readonly ILogger<ChatController> _log;

    public ChatController(
        IConversationService service,
        ITenantContext tenantCtx,
        IAiSttProvider stt,
        Infrastructure.Services.OpenApiService webhooks,
        ILogger<ChatController> log)
    {
        _service = service;
        _tenantCtx = tenantCtx;
        _stt = stt;
        _webhooks = webhooks;
        _log = log;
    }

    /// <summary>
    /// 内部/测试用：客户端直接发起对话（无需微信回调）
    /// 限流：trial 100/h、pro 500/h、enterprise 5000/h
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("chat-tenant")]
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

    /// <summary>
    /// 语音消息上传（multipart/form-data）
    /// 字段：audio (file)、tenantId、customerId、conversationId?、format?
    /// </summary>
    [HttpPost("voice")]
    [AllowAnonymous]
    [EnableRateLimiting("chat-tenant")]
    [RequestSizeLimit(20_000_000)] // 20 MB
    public async Task<ActionResult<VoiceChatResponse>> SendVoice(
        [FromForm] Guid tenantId,
        [FromForm] Guid customerId,
        [FromForm] Guid? conversationId,
        [FromForm] string format = "wav",
        IFormFile? audio = null,
        CancellationToken ct = default)
    {
        if (audio == null || audio.Length == 0)
            return BadRequest(new { message = "audio file required" });

        await using var stream = audio.OpenReadStream();
        var sttResult = await _stt.RecognizeAsync(stream, format, ct);
        AppMeter.SttCalls.Add(1, new KeyValuePair<string, object?>("provider", _stt.ProviderName));

        _log.LogInformation("语音转写: provider={Provider} text={Text}", _stt.ProviderName, sttResult.Text);

        // 触发 webhook 事件
        await _webhooks.PublishAsync(tenantId, "message.voice_received", new
        {
            tenantId, customerId, durationSeconds = sttResult.DurationSeconds,
            transcript = sttResult.Text, provider = _stt.ProviderName
        }, ct);

        // 复用现有 chat pipeline（把转写文本当成 user content）
        var chatResp = await _service.HandleUserMessageAsync(
            tenantId, customerId, sttResult.Text, conversationId, ct);

        return Ok(new VoiceChatResponse(
            Transcript: sttResult.Text,
            SttProvider: _stt.ProviderName,
            SttLatencyMs: sttResult.LatencyMs,
            Reply: chatResp.Reply,
            ConversationId: chatResp.ConversationId,
            MessageId: chatResp.MessageId,
            LatencyMs: chatResp.LatencyMs
        ));
    }
}

public record VoiceChatResponse(
    string Transcript,
    string SttProvider,
    int SttLatencyMs,
    string Reply,
    Guid ConversationId,
    Guid MessageId,
    int LatencyMs
);

public record InternalChatRequest(
    Guid TenantId,
    Guid CustomerId,
    string Content,
    Guid? ConversationId = null
);