using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SupportDesk.Application.Auth;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Infrastructure.Services;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Infrastructure.Services;

public sealed class JwtTokenIssuerTests
{
    [Fact]
    public async Task Issue_SignsTokenWithExpectedIdentityClaimsAndLifetime()
    {
        var options = new JwtOptions
        {
            Key = "0123456789abcdef0123456789abcdef",
            Issuer = "test-issuer",
            Audience = "test-audience",
            LifetimeMinutes = 60
        };
        var now = FixedClock.DefaultNow;
        var agent = new Agent("Alex Turner", "alex@example.test", 10, now);
        var issuer = new JwtTokenIssuer(options);

        var issued = issuer.Issue(agent, now);
        var handler = new JsonWebTokenHandler();
        var validation = await handler.ValidateTokenAsync(issued.Value, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
            ValidateLifetime = false
        });

        Assert.True(validation.IsValid);
        Assert.Equal(now.AddMinutes(60), issued.ExpiresAtUtc);
        Assert.Equal("0", validation.Claims[AuthClaims.Subject].ToString());
        Assert.Equal(agent.Email, validation.Claims[AuthClaims.Email].ToString());
        Assert.Equal(agent.FullName, validation.Claims[AuthClaims.Name].ToString());
    }
}
