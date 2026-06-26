using System.Security.Cryptography;
using System.Text;
using AiCustomerService.Core.DTOs.Auth;
using AiCustomerService.Core.Entities;
using AiCustomerService.Core.Exceptions;
using AiCustomerService.Core.Interfaces;
using AiCustomerService.Infrastructure.Data;
using AiCustomerService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace AiCustomerService.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly JwtTokenService _jwt;

    public AuthService(AppDbContext db, JwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (await _db.Users.AnyAsync(u => u.Username == request.Username, ct))
            throw new ValidationException("用户名已存在");

        var tenant = new Tenant
        {
            Name = request.TenantName,
            ContactName = request.ContactName,
            ContactPhone = request.ContactPhone,
            ContactEmail = request.ContactEmail,
            Plan = "trial",
            Status = "active",
            MonthlyMessageQuota = 1000,
            Settings = "{}",
            IndustryCode = request.IndustryCode ?? "general",
            TrialEndsAt = DateTime.UtcNow.AddDays(14),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);

        var user = new User
        {
            TenantId = tenant.Id,
            Username = request.Username,
            PasswordHash = HashPassword(request.Password),
            Email = request.ContactEmail,
            DisplayName = request.ContactName ?? request.Username,
            Role = "owner",
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return IssueToken(user, tenant);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Username == request.Username, ct);

        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Auth.LoginFailed");

        if (user.Status != "active")
            throw new ForbiddenException("Auth.Forbidden");

        if (user.Tenant.Status != "active")
            throw new ForbiddenException("Auth.TenantDisabled");

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return IssueToken(user, user.Tenant);
    }

    public async Task<LoginResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var stored = await _db.RefreshTokens
            .Include(r => r.User).ThenInclude(u => u.Tenant)
            .FirstOrDefaultAsync(r => r.Token == refreshToken, ct);

        if (stored == null || stored.ExpiresAt < DateTime.UtcNow || stored.RevokedAt != null)
            throw new UnauthorizedException("Auth.Unauthorized");

        stored.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return IssueToken(stored.User, stored.User.Tenant);
    }

    private LoginResponse IssueToken(User user, Tenant tenant)
    {
        var (accessToken, expiresAt) = _jwt.GenerateAccessToken(user);
        var refreshToken = _jwt.GenerateRefreshToken();
        var refreshExpiry = DateTime.UtcNow.AddDays(30);

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = refreshExpiry,
            CreatedAt = DateTime.UtcNow
        });
        _db.SaveChanges();

        return new LoginResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: expiresAt,
            User: new UserInfo(user.Id, user.TenantId, user.Username, user.DisplayName ?? user.Username, user.Role)
        );
    }

    private static string HashPassword(string password)
    {
        var salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split('.');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}