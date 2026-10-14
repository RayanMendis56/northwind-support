using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportDesk.Application.Auth;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Auth.Login;

namespace SupportDesk.Presentation.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    /// <summary>Signs an agent in. Wrong email and wrong password give the same 401.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public Task<LoginResponse> Login(
        LoginRequest request, [FromServices] LoginCommandHandler handler, CancellationToken ct) =>
        handler.HandleAsync(request, ct);

    /// <summary>The signed-in agent, from the token's claims.</summary>
    [HttpGet("me")]
    public CurrentUserDto Me()
    {
        var subject = User.FindFirst(AuthClaims.Subject)?.Value;
        var email = User.FindFirst(AuthClaims.Email)?.Value;
        var name = User.FindFirst(AuthClaims.Name)?.Value;

        if (!int.TryParse(subject, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            || email is null || name is null)
        {
            throw new AuthenticationFailedException("The token does not identify an agent.");
        }

        return new CurrentUserDto(id, name, email);
    }
}
