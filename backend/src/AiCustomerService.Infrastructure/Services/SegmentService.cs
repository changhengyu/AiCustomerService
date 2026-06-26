using System.Text.Json;
using AiCustomerService.Core.Entities;
using AiCustomerService.Core.Exceptions;
using AiCustomerService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AiCustomerService.Infrastructure.Services;

/// <summary>分群评估：根据规则判断客户是否属于某 segment</summary>
public class SegmentService
{
    private readonly AppDbContext _db;

    public SegmentService(AppDbContext db) { _db = db; }

    public async Task<List<CustomerSegment>> ListAsync(Guid tenantId, CancellationToken ct = default)
        => await _db.CustomerSegments
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    public async Task<CustomerSegment> CreateAsync(
        Guid tenantId, string name, string? description, string rulesJson, CancellationToken ct = default)
    {
        var s = new CustomerSegment
        {
            TenantId = tenantId,
            Name = name,
            Description = description,
            Rules = rulesJson
        };
        _db.CustomerSegments.Add(s);
        await _db.SaveChangesAsync(ct);
        return s;
    }

    public async Task<bool> DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        var s = await _db.CustomerSegments.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (s == null) return false;
        _db.CustomerSegments.Remove(s);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>评估单个客户是否命中某 segment</summary>
    public bool Matches(Customer c, string rulesJson)
    {
        if (string.IsNullOrEmpty(rulesJson)) return false;
        Dictionary<string, JsonElement> rules;
        try { rules = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(rulesJson) ?? new(); }
        catch { return false; }

        if (rules.TryGetValue("intention", out var intention) && intention.ValueKind == JsonValueKind.Array)
        {
            var arr = intention.EnumerateArray().Select(e => e.GetString()).ToList();
            if (!arr.Contains(c.IntentionLevel)) return false;
        }
        if (rules.TryGetValue("lifecycle", out var lc) && lc.ValueKind == JsonValueKind.Array)
        {
            var arr = lc.EnumerateArray().Select(e => e.GetString()).ToList();
            if (!arr.Contains(c.LifecycleStage)) return false;
        }
        if (rules.TryGetValue("region", out var region) && region.ValueKind == JsonValueKind.Array)
        {
            var arr = region.EnumerateArray().Select(e => e.GetString()).ToList();
            if (c.Region == null || !arr.Contains(c.Region)) return false;
        }
        if (rules.TryGetValue("tags", out var tags) && tags.ValueKind == JsonValueKind.Array)
        {
            var arr = tags.EnumerateArray().Select(e => e.GetString()).Where(s => s != null).Cast<string>().ToList();
            if (!arr.Any(t => c.Tags.Contains(t))) return false;
        }
        if (rules.TryGetValue("min_score", out var minScore) && minScore.ValueKind == JsonValueKind.Number)
        {
            if (c.IntentionScore < minScore.GetInt32()) return false;
        }
        return true;
    }

    /// <summary>重新计算某 segment 的 member_count</summary>
    public async Task<int> RecomputeAsync(Guid segmentId, CancellationToken ct = default)
    {
        var seg = await _db.CustomerSegments.FindAsync(new object[] { segmentId }, ct)
            ?? throw new NotFoundException("Customer.NotFound");
        var customers = await _db.Customers
            .Where(c => c.TenantId == seg.TenantId)
            .ToListAsync(ct);
        var count = customers.Count(c => Matches(c, seg.Rules));
        seg.MemberCount = count;
        seg.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return count;
    }

    /// <summary>列出某 segment 的成员</summary>
    public async Task<List<Customer>> GetMembersAsync(Guid segmentId, int limit = 200, CancellationToken ct = default)
    {
        var seg = await _db.CustomerSegments.FindAsync(new object[] { segmentId }, ct)
            ?? throw new NotFoundException("Customer.NotFound");
        var customers = await _db.Customers
            .Where(c => c.TenantId == seg.TenantId)
            .Take(limit * 4) // overshoot filter
            .ToListAsync(ct);
        return customers.Where(c => Matches(c, seg.Rules)).Take(limit).ToList();
    }

    /// <summary>每日重新计算全部动态 segment</summary>
    public async Task RecomputeAllAsync(CancellationToken ct = default)
    {
        var segments = await _db.CustomerSegments.Where(s => s.IsDynamic).ToListAsync(ct);
        foreach (var seg in segments)
        {
            var customers = await _db.Customers.Where(c => c.TenantId == seg.TenantId).ToListAsync(ct);
            seg.MemberCount = customers.Count(c => Matches(c, seg.Rules));
            seg.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }
}
