using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Auth;
using SupportDesk.Domain.Aggregates.Agents;

namespace SupportDesk.Infrastructure.Services;

public sealed class JwtTokenIssuer(JwtOptions options) : ITokenIssuer
{
    public IssuedToken Issue(Agent agent, DateTime nowUtc)
    {
        var expires = nowUtc.AddMinutes(options.LifetimeMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(AuthClaims.Subject, agent.Id.ToString(CultureInfo.InvariantCulture)),
                new Claim(AuthClaims.Email, agent.Email),
                new Claim(AuthClaims.Name, agent.FullName)
            ]),
            Issuer = options.Issuer,
            Audience = options.Audience,
            NotBefore = nowUtc,
            IssuedAt = nowUtc,
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
                SecurityAlgorithms.HmacSha256)
        };

        return new IssuedToken(new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }
}
