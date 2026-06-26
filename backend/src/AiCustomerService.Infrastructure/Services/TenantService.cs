using System.Text.Json;
using AiCustomerService.Core.Entities;
using AiCustomerService.Core.Exceptions;
using AiCustomerService.Core.Interfaces;
using AiCustomerService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AiCustomerService.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly AppDbContext _db;

    public TenantService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Tenant?> GetCurrentAsync(Guid tenantId, CancellationToken ct = default)
        => await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);

    public async Task<Tenant?> GetByWeChatAppIdAsync(string appId, CancellationToken ct = default)
    {
        // 通过 ChannelConfig 表查找
        var ch = await _db.ChannelConfigs
            .Include(c => c.Tenant)
            .FirstOrDefaultAsync(c => c.ChannelType == "wechat" && c.AppId == appId, ct);
        return ch?.Tenant;
    }

    public async Task UpdateSettingsAsync(Guid tenantId, TenantSettingsDto settings, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new NotFoundException("租户不存在");
        tenant.Settings = JsonSerializer.Serialize(settings);
        tenant.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<TenantSettingsDto> GetSettingsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant == null || string.IsNullOrEmpty(tenant.Settings))
            return new TenantSettingsDto("你是 AI 客服。", "您好，请问有什么可以帮您？",
                Array.Empty<string>(), null, false);

        try
        {
            return JsonSerializer.Deserialize<TenantSettingsDto>(tenant.Settings)
                ?? new TenantSettingsDto("你是 AI 客服。", "您好，请问有什么可以帮您？",
                    Array.Empty<string>(), null, false);
        }
        catch
        {
            return new TenantSettingsDto("你是 AI 客服。", "您好，请问有什么可以帮您？",
                Array.Empty<string>(), null, false);
        }
    }
}