using System.Text.Json;
using AiCustomerService.Core.Entities;
using AiCustomerService.Core.Exceptions;
using AiCustomerService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AiCustomerService.Infrastructure.Services;

/// <summary>客户画像服务 — profile / notes / timeline</summary>
public class ProfileService
{
    private readonly AppDbContext _db;

    public ProfileService(AppDbContext db) { _db = db; }

    public async Task<CustomerProfileDto> GetProfileAsync(Guid tenantId, Guid customerId, CancellationToken ct = default)
    {
        var c = await _db.Customers
            .FirstOrDefaultAsync(x => x.Id == customerId && x.TenantId == tenantId, ct)
            ?? throw new NotFoundException("Customer.NotFound");

        var notes = await _db.CustomerNotes
            .Where(n => n.CustomerId == customerId && n.TenantId == tenantId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new CustomerNoteDto(n.Id, n.Content, n.AuthorUserId, n.CreatedAt))
            .ToListAsync(ct);

        var timeline = await _db.CustomerTimelineEvents
            .Where(t => t.CustomerId == customerId && t.TenantId == tenantId)
            .OrderByDescending(t => t.OccurredAt)
            .Take(100)
            .Select(t => new TimelineEventDto(t.Id, t.EventType, t.Payload, t.OccurredAt))
            .ToListAsync(ct);

        return new CustomerProfileDto(
            Id: c.Id,
            Nickname: c.Nickname,
            AvatarUrl: c.AvatarUrl,
            Phone: c.Phone,
            Email: c.Email,
            Gender: c.Gender,
            Region: c.Region,
            Source: c.Source,
            LifecycleStage: c.LifecycleStage,
            IntentionLevel: c.IntentionLevel,
            IntentionScore: c.IntentionScore,
            Tags: c.Tags,
            Metadata: c.Metadata,
            FirstSeenAt: c.FirstSeenAt,
            LastSeenAt: c.LastSeenAt,
            LastProfileUpdateAt: c.LastProfileUpdateAt,
            Completeness: ComputeCompleteness(c),
            Notes: notes,
            Timeline: timeline
        );
    }

    public async Task UpdateProfileAsync(Guid tenantId, Guid customerId, UpdateProfileRequest req, CancellationToken ct = default)
    {
        var c = await _db.Customers
            .FirstOrDefaultAsync(x => x.Id == customerId && x.TenantId == tenantId, ct)
            ?? throw new NotFoundException("Customer.NotFound");

        if (req.Email != null) c.Email = req.Email;
        if (req.Nickname != null) c.Nickname = req.Nickname;
        if (req.Phone != null) c.Phone = req.Phone;
        if (req.Region != null) c.Region = req.Region;
        if (req.Gender != null) c.Gender = req.Gender;
        if (req.Source != null) c.Source = req.Source;
        if (req.LifecycleStage != null) c.LifecycleStage = req.LifecycleStage;
        if (req.Tags != null) c.Tags = req.Tags;
        c.LastProfileUpdateAt = DateTime.UtcNow;

        AppendTimeline(tenantId, customerId, "customer.profile_updated",
            JsonSerializer.Serialize(new { fields = req.GetType().GetProperties().Select(p => p.Name) }));
        await _db.SaveChangesAsync(ct);
    }

    public async Task<CustomerNoteDto> AddNoteAsync(
        Guid tenantId, Guid customerId, string content, Guid? authorUserId, CancellationToken ct = default)
    {
        var n = new CustomerNote
        {
            TenantId = tenantId,
            CustomerId = customerId,
            AuthorUserId = authorUserId,
            Content = content
        };
        _db.CustomerNotes.Add(n);
        AppendTimeline(tenantId, customerId, "customer.note_added", JsonSerializer.Serialize(new { noteId = n.Id }));
        await _db.SaveChangesAsync(ct);
        return new CustomerNoteDto(n.Id, n.Content, n.AuthorUserId, n.CreatedAt);
    }

    public void AppendTimeline(Guid tenantId, Guid customerId, string eventType, string payload)
    {
        _db.CustomerTimelineEvents.Add(new CustomerTimelineEvent
        {
            TenantId = tenantId,
            CustomerId = customerId,
            EventType = eventType,
            Payload = payload
        });
    }

    private static int ComputeCompleteness(Customer c)
    {
        // 0-100：每个核心字段占一定权重
        var score = 0;
        if (!string.IsNullOrEmpty(c.Nickname)) score += 15;
        if (!string.IsNullOrEmpty(c.Email)) score += 20;
        if (!string.IsNullOrEmpty(c.Phone)) score += 25;
        if (!string.IsNullOrEmpty(c.Region)) score += 10;
        if (c.Tags.Length > 0) score += 15;
        if (c.IntentionScore > 0) score += 15;
        return Math.Min(100, score);
    }
}

public record UpdateProfileRequest(
    string? Email = null,
    string? Nickname = null,
    string? Phone = null,
    string? Region = null,
    string? Gender = null,
    string? Source = null,
    string? LifecycleStage = null,
    string[]? Tags = null
);

public record CustomerProfileDto(
    Guid Id,
    string? Nickname,
    string? AvatarUrl,
    string? Phone,
    string? Email,
    string? Gender,
    string? Region,
    string? Source,
    string LifecycleStage,
    string IntentionLevel,
    int IntentionScore,
    string[] Tags,
    string Metadata,
    DateTime FirstSeenAt,
    DateTime LastSeenAt,
    DateTime? LastProfileUpdateAt,
    int Completeness,
    List<CustomerNoteDto> Notes,
    List<TimelineEventDto> Timeline
);

public record CustomerNoteDto(Guid Id, string Content, Guid? AuthorUserId, DateTime CreatedAt);

public record TimelineEventDto(Guid Id, string EventType, string Payload, DateTime OccurredAt);
