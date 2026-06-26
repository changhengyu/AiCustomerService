using System.Text.Json;
using AiCustomerService.Core.Entities;
using AiCustomerService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiCustomerService.Infrastructure.Services;

/// <summary>营销触发器：监听事件并自动执行动作</summary>
public class MarketingTriggerService
{
    private readonly AppDbContext _db;
    private readonly OpenApiService _webhooks;
    private readonly ILogger<MarketingTriggerService> _log;

    public MarketingTriggerService(
        AppDbContext db, OpenApiService webhooks, ILogger<MarketingTriggerService> log)
    { _db = db; _webhooks = webhooks; _log = log; }

    public async Task<List<MarketingTrigger>> ListAsync(Guid tenantId, CancellationToken ct = default)
        => await _db.MarketingTriggers
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task<MarketingTrigger> CreateAsync(
        Guid tenantId, string name, string eventType, string conditions, string actions, CancellationToken ct = default)
    {
        var t = new MarketingTrigger
        {
            TenantId = tenantId,
            Name = name,
            EventType = eventType,
            Conditions = conditions,
            Actions = actions
        };
        _db.MarketingTriggers.Add(t);
        await _db.SaveChangesAsync(ct);
        return t;
    }

    public async Task<bool> DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        var t = await _db.MarketingTriggers.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (t == null) return false;
        _db.MarketingTriggers.Remove(t);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task OnEventAsync(Guid tenantId, string eventType, object payload, CancellationToken ct = default)
    {
        var triggers = await _db.MarketingTriggers
            .Where(t => t.TenantId == tenantId && t.Active && t.EventType == eventType)
            .ToListAsync(ct);

        var payloadJson = JsonSerializer.Serialize(payload);
        foreach (var t in triggers)
        {
            try
            {
                if (!MatchConditions(payloadJson, t.Conditions)) continue;
                await ExecuteActionsAsync(tenantId, t.Actions, payloadJson, ct);
                _log.LogInformation("MarketingTrigger {Name} fired for {Event}", t.Name, eventType);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Trigger {Name} failed", t.Name);
            }
        }
    }

    private static bool MatchConditions(string payloadJson, string conditionsJson)
    {
        if (string.IsNullOrWhiteSpace(conditionsJson) || conditionsJson == "{}") return true;
        try
        {
            var conds = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(conditionsJson);
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson);
            if (conds == null || payload == null) return true;
            foreach (var (k, v) in conds)
            {
                if (!payload.TryGetValue(k, out var pv)) return false;
                if (v.ValueKind == JsonValueKind.String && pv.ValueKind == JsonValueKind.String)
                {
                    if (pv.GetString() != v.GetString()) return false;
                }
            }
            return true;
        }
        catch { return true; }
    }

    private async Task ExecuteActionsAsync(Guid tenantId, string actionsJson, string payloadJson, CancellationToken ct)
    {
        var actions = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(actionsJson);
        if (actions == null) return;

        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson) ?? new();

        foreach (var action in actions)
        {
            if (!action.TryGetValue("type", out var typeEl)) continue;
            var type = typeEl.GetString();
            switch (type)
            {
                case "add_tag":
                    if (payload.TryGetValue("customerId", out var cid) &&
                        action.TryGetValue("tag", out var tag))
                    {
                        var customerId = cid.GetGuid();
                        var c = await _db.Customers.FirstOrDefaultAsync(
                            x => x.Id == customerId && x.TenantId == tenantId, ct);
                        if (c != null && !c.Tags.Contains(tag.GetString()!))
                        {
                            c.Tags = c.Tags.Append(tag.GetString()!).ToArray();
                            c.LastProfileUpdateAt = DateTime.UtcNow;
                            await _db.SaveChangesAsync(ct);
                        }
                    }
                    break;

                case "add_note":
                    if (payload.TryGetValue("customerId", out var cid2) &&
                        action.TryGetValue("content", out var content))
                    {
                        var n = new CustomerNote
                        {
                            TenantId = tenantId,
                            CustomerId = cid2.GetGuid(),
                            Content = content.GetString()!,
                            AuthorUserId = null
                        };
                        _db.CustomerNotes.Add(n);
                        await _db.SaveChangesAsync(ct);
                    }
                    break;

                case "webhook":
                    await _webhooks.PublishAsync(tenantId, "marketing.action_executed", new
                    {
                        action = type,
                        payload
                    }, ct);
                    break;
            }
        }
    }
}
