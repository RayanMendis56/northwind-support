namespace SupportDesk.Application.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record CurrentUserDto(int Id, string FullName, string Email);

public sealed record LoginResponse(string Token, DateTime ExpiresAtUtc, CurrentUserDto User);

public sealed record IssuedToken(string Value, DateTime ExpiresAtUtc);

/// <summary>Claim names shared by the issuer and by whatever reads the token.</summary>
public static class AuthClaims
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Name = "name";
}

/// <summary>Bound from the "Jwt" configuration section. The key has no default on purpose.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinimumKeyLength = 32;

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "northwind-support";
    public string Audience { get; set; } = "northwind-support-web";
    public int LifetimeMinutes { get; set; } = 60;

    public void Validate()
    {
        if (Key.Length < MinimumKeyLength)
        {
            throw new InvalidOperationException(
                $"Jwt:Key must be set (environment variable Jwt__Key) and be at least {MinimumKeyLength} characters.");
        }

        if (LifetimeMinutes <= 0)
        {
            throw new InvalidOperationException("Jwt:LifetimeMinutes must be positive.");
        }
    }
}
