namespace AiCustomerService.Api.Localization;

/// <summary>资源 key 常量 — 便于重构和静态分析</summary>
public static class LocalizedStrings
{
    public static class Auth
    {
        public const string LoginFailed = "Auth.LoginFailed";
        public const string Unauthorized = "Auth.Unauthorized";
        public const string Forbidden = "Auth.Forbidden";
        public const string UserExists = "Auth.UserExists";
        public const string TenantDisabled = "Auth.TenantDisabled";
    }
    public static class Tenant
    {
        public const string NotFound = "Tenant.NotFound";
        public const string QuotaExceeded = "Tenant.QuotaExceeded";
    }
    public static class Customer
    {
        public const string NotFound = "Customer.NotFound";
    }
    public static class Conversation
    {
        public const string NotFound = "Conversation.NotFound";
    }
    public static class Knowledge
    {
        public const string NotFound = "Knowledge.NotFound";
        public const string UploadFailed = "Knowledge.UploadFailed";
    }
    public static class Common
    {
        public const string ValidationFailed = "Common.ValidationFailed";
        public const string InternalError = "Common.InternalError";
        public const string RateLimited = "Common.RateLimited";
    }
}
