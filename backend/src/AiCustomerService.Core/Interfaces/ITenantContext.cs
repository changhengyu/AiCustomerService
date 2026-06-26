namespace AiCustomerService.Core.Interfaces;

public interface ITenantContext
{
    Guid? CurrentTenantId { get; }
    Guid RequireTenantId();
    Guid? CurrentUserId { get; }
    string? CurrentRole { get; }
    IDisposable OverrideTenant(Guid tenantId);
}