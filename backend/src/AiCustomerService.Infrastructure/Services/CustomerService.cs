using AiCustomerService.Core.DTOs.Knowledge;
using AiCustomerService.Core.Entities;
using AiCustomerService.Core.Exceptions;
using AiCustomerService.Core.Interfaces;
using AiCustomerService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AiCustomerService.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _db;

    public CustomerService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AiCustomerService.Core.Interfaces.CustomerListItemDto>> ListAsync(
        Guid tenantId, int page, int pageSize,
        string? intentionLevel = null, string? keyword = null, CancellationToken ct = default)
    {
        var query = _db.Customers.Where(c => c.TenantId == tenantId);

        if (!string.IsNullOrEmpty(intentionLevel))
            query = query.Where(c => c.IntentionLevel == intentionLevel);

        if (!string.IsNullOrEmpty(keyword))
            query = query.Where(c =>
                (c.Nickname != null && c.Nickname.Contains(keyword)) ||
                (c.ExternalId != null && c.ExternalId.Contains(keyword)));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.LastSeenAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new AiCustomerService.Core.Interfaces.CustomerListItemDto(
                c.Id, c.Nickname, c.AvatarUrl, c.ChannelType, c.IntentionLevel,
                c.IntentionScore, c.Tags, c.LastSeenAt
            ))
            .ToListAsync(ct);

        return new PagedResult<AiCustomerService.Core.Interfaces.CustomerListItemDto>(items, total, page, pageSize);
    }

    public async Task<AiCustomerService.Core.Interfaces.CustomerDetailDto?> GetDetailAsync(
        Guid tenantId, Guid customerId, CancellationToken ct = default)
    {
        var c = await _db.Customers
            .FirstOrDefaultAsync(x => x.Id == customerId && x.TenantId == tenantId, ct);
        if (c == null) return null;

        var convCount = await _db.Conversations.CountAsync(x => x.CustomerId == customerId, ct);

        return new AiCustomerService.Core.Interfaces.CustomerDetailDto(
            Id: c.Id,
            Nickname: c.Nickname,
            AvatarUrl: c.AvatarUrl,
            Phone: c.Phone,
            Region: c.Region,
            ChannelType: c.ChannelType,
            IntentionLevel: c.IntentionLevel,
            IntentionScore: c.IntentionScore,
            Tags: c.Tags,
            Metadata: c.Metadata,
            FirstSeenAt: c.FirstSeenAt,
            LastSeenAt: c.LastSeenAt,
            TotalConversations: convCount
        );
    }

    public async Task UpdateTagsAsync(
        Guid tenantId, Guid customerId, string[] tags, CancellationToken ct = default)
    {
        var c = await _db.Customers.FirstOrDefaultAsync(x => x.Id == customerId && x.TenantId == tenantId, ct)
            ?? throw new NotFoundException("Customer.NotFound");
        c.Tags = tags ?? Array.Empty<string>();
        await _db.SaveChangesAsync(ct);
    }
}