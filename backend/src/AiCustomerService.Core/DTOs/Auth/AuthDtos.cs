namespace AiCustomerService.Core.DTOs.Auth;

public record RegisterRequest(
    string TenantName,
    string Username,
    string Password,
    string? ContactName,
    string? ContactPhone,
    string? ContactEmail,
    string IndustryCode
);

public record LoginRequest(string Username, string Password, Guid TenantId);

public record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserInfo User);

public record UserInfo(Guid Id, Guid TenantId, string Username, string DisplayName, string Role);

public record RefreshTokenRequest(string RefreshToken);
