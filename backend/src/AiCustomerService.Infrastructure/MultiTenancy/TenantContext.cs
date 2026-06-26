using System.Security.Claims;
using AiCustomerService.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AiCustomerService.Infrastructure.MultiTenancy;

/// <summary>
/// 当前租户上下文：从 JWT Claims 提取 TenantId
/// </summary>
public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid? _overrideTenantId;

    public TenantContext(IHttpContextAccessor accessor)
    {
        _httpContextAccessor = accessor;
    }

    public Guid? CurrentTenantId
    {
        get
        {
            if (_overrideTenantId.HasValue) return _overrideTenantId;

            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity?.IsAuthenticated == true) return null;

            var claim = user.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public Guid RequireTenantId()
    {
        var id = CurrentTenantId;
        if (id == null) throw new UnauthorizedAccessException("未识别租户");
        return id.Value;
    }

    public Guid? CurrentUserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var claim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public string? CurrentRole =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;

    public IDisposable OverrideTenant(Guid tenantId)
    {
        _overrideTenantId = tenantId;
        return new Restorer(() => _overrideTenantId = null);
    }

    private class Restorer : IDisposable
    {
        private readonly Action _action;
        public Restorer(Action action) { _action = action; }
        public void Dispose() => _action();
    }
}