using System.Globalization;
using System.Text.Json;
using AiCustomerService.Api.Resources;
using AiCustomerService.Core.Exceptions;
using Microsoft.Extensions.Localization;

namespace AiCustomerService.Api.Localization;

/// <summary>
/// 全局异常中间件：把 ApiException 的 message 当作 resource key，
/// 用当前 culture 翻译后返回给前端。
/// </summary>
public class LocalizedExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LocalizedExceptionMiddleware> _log;

    public LocalizedExceptionMiddleware(RequestDelegate next, ILogger<LocalizedExceptionMiddleware> log)
    { _next = next; _log = log; }

    public async Task InvokeAsync(HttpContext ctx, IStringLocalizer<SharedResource> localizer)
    {
        try
        {
            await _next(ctx);
        }
        catch (BusinessException ex)
        {
            // ex.Message 通常是 resource key；找不到就 fallback 到原 message
            var localized = localizer[ex.Message];
            var message = localized.ResourceNotFound ? ex.Message : localized.Value;
            await WriteError(ctx, ex.Code, message);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "未捕获异常: {Path}", ctx.Request.Path);
            var key = ex.GetType().Name switch
            {
                nameof(NotFoundException) => "Common.InternalError",
                _ => "Common.InternalError"
            };
            var localized = localizer[key];
            await WriteError(ctx, 500, localized.Value);
        }
    }

    private static async Task WriteError(HttpContext ctx, int code, string message)
    {
        ctx.Response.StatusCode = code switch
        {
            401 => StatusCodes.Status401Unauthorized,
            403 => StatusCodes.Status403Forbidden,
            404 => StatusCodes.Status404NotFound,
            422 => StatusCodes.Status422UnprocessableEntity,
            429 => StatusCodes.Status429TooManyRequests,
            _ => StatusCodes.Status500InternalServerError
        };
        ctx.Response.ContentType = "application/json; charset=utf-8";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            code = code,
            message = message,
            trace_id = ctx.TraceIdentifier
        }));
    }
}
