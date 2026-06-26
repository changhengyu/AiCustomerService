using System.ComponentModel;
using System.Diagnostics;
using AiCustomerService.Core.Entities;
using AiCustomerService.Core.Interfaces;
using AiCustomerService.Infrastructure.Data;
using AiCustomerService.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AiCustomerService.Infrastructure.AI.Agent;

/// <summary>
/// AI 智能体可调用的工具集（Function Calling）。
/// 每个公开方法会被包装成 AIFunction，注入到 ChatClient 的 ChatOptions.Tools 中。
/// </summary>
public class CustomerServiceTools
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantCtx;
    private readonly ILogger<CustomerServiceTools> _logger;

    public CustomerServiceTools(
        AppDbContext db,
        ITenantContext tenantCtx,
        ILogger<CustomerServiceTools> logger)
    {
        _db = db;
        _tenantCtx = tenantCtx;
        _logger = logger;
    }

    [Description("根据订单号查询订单状态（仅查询本租户订单）")]
    public async Task<string> QueryOrderAsync(
        [Description("订单号")] string orderNo)
    {
        using var span = AppActivitySource.Source.StartActivity("tool.query_order");
        span?.SetTag("tool.input.order_no", orderNo);
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("AI 调用工具 QueryOrder: {OrderNo}", orderNo);
        var tenantId = _tenantCtx.RequireTenantId();
        await Task.Delay(10);
        sw.Stop();
        AppMeter.ToolCallLatency.Record(sw.ElapsedMilliseconds,
            new KeyValuePair<string, object?>("tool", "query_order"));
        return $"订单 {orderNo} 状态：已发货。预计 2026-06-30 送达。";
    }

    [Description("根据订单号查询物流轨迹")]
    public async Task<string> QueryLogisticsAsync(
        [Description("订单号")] string orderNo)
    {
        using var span = AppActivitySource.Source.StartActivity("tool.query_logistics");
        span?.SetTag("tool.input.order_no", orderNo);
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("AI 调用工具 QueryLogistics: {OrderNo}", orderNo);
        var tenantId = _tenantCtx.RequireTenantId();
        await Task.Delay(10);
        sw.Stop();
        AppMeter.ToolCallLatency.Record(sw.ElapsedMilliseconds,
            new KeyValuePair<string, object?>("tool", "query_logistics"));
        return $"订单 {orderNo} 物流：已到达【杭州转运中心】，下一站【上海浦东】";
    }

    [Description("为指定订单申请退款（金额 + 原因）")]
    public async Task<string> RequestRefundAsync(
        [Description("订单号")] string orderNo,
        [Description("退款金额（元）")] decimal amount,
        [Description("退款原因")] string reason)
    {
        using var span = AppActivitySource.Source.StartActivity("tool.request_refund");
        span?.SetTag("tool.input.order_no", orderNo);
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("AI 调用工具 RequestRefund: Order={OrderNo}, Amount={Amount}",
            orderNo, amount);
        await Task.Delay(10);
        sw.Stop();
        AppMeter.ToolCallLatency.Record(sw.ElapsedMilliseconds,
            new KeyValuePair<string, object?>("tool", "request_refund"));
        return $"退款申请已提交：订单 {orderNo}，金额 ¥{amount}，预计 1-3 个工作日原路退回。";
    }

    [Description("将会话转接给人工客服（立即结束 AI 应答）")]
    public async Task<string> HandoffToHumanAsync(
        [Description("转人工原因")] string reason)
    {
        using var span = AppActivitySource.Source.StartActivity("tool.handoff_to_human");
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("AI 调用工具 HandoffToHuman: {Reason}", reason);
        await Task.Delay(10);
        sw.Stop();
        AppMeter.ToolCallLatency.Record(sw.ElapsedMilliseconds,
            new KeyValuePair<string, object?>("tool", "handoff_to_human"));
        return "已为您转接人工客服，请稍候，客服将尽快接入。";
    }

    [Description("根据客户手机号查询其历史订单与互动记录")]
    public async Task<string> QueryCustomerHistoryAsync(
        [Description("客户手机号或外部 ID")] string externalId)
    {
        using var span = AppActivitySource.Source.StartActivity("tool.query_customer_history");
        span?.SetTag("tool.input.external_id", externalId);
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("AI 调用工具 QueryCustomerHistory: {Id}", externalId);
        var tenantId = _tenantCtx.RequireTenantId();
        var customer = await _db.Customers.FirstOrDefaultAsync(
            c => c.TenantId == tenantId && c.ExternalId == externalId);
        if (customer == null) return $"未找到客户 {externalId}";
        var orders = await _db.Conversations
            .Where(c => c.CustomerId == customer.Id)
            .CountAsync();
        sw.Stop();
        AppMeter.ToolCallLatency.Record(sw.ElapsedMilliseconds,
            new KeyValuePair<string, object?>("tool", "query_customer_history"));
        return $"客户 {customer.Nickname}（{externalId}）历史会话：{orders} 次，最后联系：{customer.LastSeenAt:yyyy-MM-dd HH:mm}";
    }
}

/// <summary>
/// 工具集扩展：将上述 C# 方法批量转为 AIFunction 列表。
/// </summary>
public static class ToolExtensions
{
    public static IList<AITool> AsAiTools(this CustomerServiceTools tools)
    {
        return new List<AITool>
        {
            AIFunctionFactory.Create(tools.QueryOrderAsync, name: "query_order"),
            AIFunctionFactory.Create(tools.QueryLogisticsAsync, name: "query_logistics"),
            AIFunctionFactory.Create(tools.RequestRefundAsync, name: "request_refund"),
            AIFunctionFactory.Create(tools.HandoffToHumanAsync, name: "handoff_to_human"),
            AIFunctionFactory.Create(tools.QueryCustomerHistoryAsync, name: "query_customer_history"),
        };
    }
}
