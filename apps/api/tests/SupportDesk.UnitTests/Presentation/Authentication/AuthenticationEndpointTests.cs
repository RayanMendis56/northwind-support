using System.Security.Claims;
using System.Text;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SupportDesk.Application.Auth;

namespace SupportDesk.UnitTests.Presentation.Authentication;

public sealed class AuthenticationEndpointTests : IClassFixture<AuthenticationApiFactory>
{
    private readonly HttpClient _client;

    public AuthenticationEndpointTests(AuthenticationApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsCurrentUser()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", AuthenticationApiFactory.CreateToken());

        var response = await _client.SendAsync(request);
        var user = await response.Content.ReadFromJsonAsync<CurrentUserDto>();

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new CurrentUserDto(42, "Test Agent", "agent@example.com"), user);
    }

    [Fact]
    public async Task Health_WithoutToken_IsNotBlockedByFallbackAuthorization()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.Equal("Unhealthy", body?.Status);
        Assert.Equal("Disconnected", body?.Database);
    }

    private sealed record HealthResponse(string Status, string Database);
}

public sealed class AuthenticationApiFactory : WebApplicationFactory<Program>
{
    public const string SigningKey = "integration-test-signing-key-that-is-long-enough";
    public const string Issuer = "northwind-support-test";
    public const string Audience = "northwind-support-test-client";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(
            "ConnectionStrings:Default",
            "Server=localhost;Database=AuthEndpointTests;User Id=sa;Password=Test_password-1;TrustServerCertificate=True");
        builder.UseSetting("Jwt:Key", SigningKey);
        builder.UseSetting("Jwt:Issuer", Issuer);
        builder.UseSetting("Jwt:Audience", Audience);
        builder.UseSetting("Jwt:LifetimeMinutes", "60");
        builder.UseSetting("Sla:BaseWindowHours:Low", "72");
        builder.UseSetting("Sla:BaseWindowHours:Medium", "24");
        builder.UseSetting("Sla:BaseWindowHours:High", "8");
        builder.UseSetting("Sla:BaseWindowHours:Critical", "4");
        builder.UseSetting("Sla:PremiumCustomerMultiplier", "0.5");
        builder.UseSetting("Sla:MinimumWindowHours", "1");
        builder.UseSetting("Sla:AtRiskThresholdPercent", "25");
    }

    public static string CreateToken()
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(AuthClaims.Subject, "42"),
                new Claim(AuthClaims.Email, "agent@example.com"),
                new Claim(AuthClaims.Name, "Test Agent")
            ]),
            Issuer = Issuer,
            Audience = Audience,
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
