using FluentValidation;
using Microsoft.Extensions.Logging;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Auth;
using SupportDesk.Application.Exceptions;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Auth.Login;

public sealed class LoginCommandHandler(
    IAgentRepository agents,
    IPasswordService passwords,
    ITokenIssuer tokens,
    IClock clock,
    ILogger<LoginCommandHandler> logger)
{
    /// <summary>The one message for every failure, so a caller cannot tell which half was wrong.</summary>
    public const string FailureMessage = "Invalid email or password.";

    /// <exception cref="AuthenticationFailedException">Unknown email, wrong password or inactive agent.</exception>
    public async Task<LoginResponse> HandleAsync(LoginRequest request, CancellationToken ct)
    {
        var agent = await agents.GetByEmailAsync(request.Email.Trim(), ct);
        var passwordMatches = passwords.Verify(agent, request.Password);

        if (agent is null || !passwordMatches || !agent.IsActive)
        {
            logger.LogInformation("Failed login attempt.");
            throw new AuthenticationFailedException(FailureMessage);
        }

        var token = tokens.Issue(agent, clock.UtcNow);

        return new LoginResponse(
            token.Value,
            token.ExpiresAtUtc,
            new CurrentUserDto(agent.Id, agent.FullName, agent.Email));
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
    }
}
