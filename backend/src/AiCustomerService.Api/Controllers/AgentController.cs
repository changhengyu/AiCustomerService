using AiCustomerService.Infrastructure.AI;
using AiCustomerService.Infrastructure.AI.Agent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiCustomerService.Api.Controllers;

/// <summary>
/// AI 智能体端点（v0.3.0+）— Function Calling 自动工具调用
/// </summary>
[ApiController]
[Route("api/v1/agent")]
[Authorize]
public class AgentController : ControllerBase
{
    private readonly AgentService _agent;
    private readonly IAiProviderFactory _factory;

    public AgentController(AgentService agent, IAiProviderFactory factory)
    {
        _agent = agent;
        _factory = factory;
    }

    /// <summary>智能体对话（自动选择和调用工具）</summary>
    [HttpPost("chat")]
    public async Task<ActionResult<AgentChatResponse>> Chat(
        [FromBody] AgentChatRequest request, CancellationToken ct)
    {
        var result = await _agent.ChatAsync(request, ct);
        return Ok(result);
    }

    /// <summary>列出所有可用的 LLM 提供商</summary>
    [HttpGet("providers")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ProviderInfoDto>>> ListProviders()
        => Ok(_factory.ListProviders());

    /// <summary>获取指定 provider 信息</summary>
    [HttpGet("providers/{name}")]
    [AllowAnonymous]
    public ActionResult<ProviderInfoDto> GetProvider(string name)
    {
        var p = _factory.GetProvider(name);
        return p == null ? NotFound() : Ok(p);
    }
}
