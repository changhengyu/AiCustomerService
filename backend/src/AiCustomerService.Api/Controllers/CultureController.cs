using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiCustomerService.Api.Controllers;

/// <summary>当前 culture 与支持列表</summary>
[ApiController]
[Route("api/v1/culture")]
[AllowAnonymous]
public class CultureController : ControllerBase
{
    [HttpGet("current")]
    public IActionResult Current()
    {
        var ui = System.Globalization.CultureInfo.CurrentUICulture;
        return Ok(new
        {
            culture = ui.Name,
            display_name = ui.DisplayName,
            supported = new[] { "zh-CN", "en-US" }
        });
    }
}
