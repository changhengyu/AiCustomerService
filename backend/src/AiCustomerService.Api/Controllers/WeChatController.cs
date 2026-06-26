using AiCustomerService.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AiCustomerService.Api.Controllers;

/// <summary>
/// 微信公众号回调：GET 验证 URL + POST 接收消息
/// </summary>
[ApiController]
[Route("api/v1/wechat/{appId}")]
public class WeChatController : ControllerBase
{
    private readonly IWeChatService _service;

    public WeChatController(IWeChatService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Verify(
        string appId,
        [FromQuery] string signature,
        [FromQuery] string timestamp,
        [FromQuery] string nonce,
        [FromQuery] string echostr,
        CancellationToken ct)
    {
        var result = await _service.VerifyUrlAsync(appId, signature, timestamp, nonce, echostr, ct);
        if (result == null) return Unauthorized();
        return Content(result, "text/plain");
    }

    [HttpPost]
    public async Task<IActionResult> Receive(
        string appId,
        CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var xml = await reader.ReadToEndAsync(ct);
        var reply = await _service.HandleMessageAsync(appId, xml, ct);
        return Content(reply, "application/xml");
    }
}