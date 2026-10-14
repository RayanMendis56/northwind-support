using Microsoft.Extensions.Logging.Abstractions;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Auth;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Auth.Login;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Repositories;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Application.Features.Auth.Login;

public sealed class LoginCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_TrimsEmailAndReturnsTokenAndCurrentUser()
    {
        var agent = new Agent("Alex Turner", "alex@example.test", 10, FixedClock.DefaultNow);
        var agents = new Mock<IAgentRepository>();
        agents.Setup(repository => repository.GetByEmailAsync("alex@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);
        var passwords = new Mock<IPasswordService>();
        passwords.Setup(service => service.Verify(agent, "password")).Returns(true);
        var tokens = new Mock<ITokenIssuer>();
        var issued = new IssuedToken("jwt-token", FixedClock.DefaultNow.AddMinutes(60));
        tokens.Setup(issuer => issuer.Issue(agent, FixedClock.DefaultNow)).Returns(issued);
        var handler = new LoginCommandHandler(
            agents.Object,
            passwords.Object,
            tokens.Object,
            new FixedClock(),
            NullLogger<LoginCommandHandler>.Instance);

        var response = await handler.HandleAsync(
            new LoginRequest("  alex@example.test ", "password"),
            CancellationToken.None);

        Assert.Equal(issued.Value, response.Token);
        Assert.Equal(issued.ExpiresAtUtc, response.ExpiresAtUtc);
        Assert.Equal(agent.Id, response.User.Id);
        Assert.Equal(agent.FullName, response.User.FullName);
        Assert.Equal(agent.Email, response.User.Email);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailIsUnknown_VerifiesPasswordAndUsesGenericFailure()
    {
        var agents = new Mock<IAgentRepository>();
        agents.Setup(repository => repository.GetByEmailAsync("missing@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Agent?)null);
        var passwords = new Mock<IPasswordService>();
        passwords.Setup(service => service.Verify(null, "password")).Returns(false);
        var handler = CreateHandler(agents, passwords);

        var exception = await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            handler.HandleAsync(new LoginRequest("missing@example.test", "password"), CancellationToken.None));

        Assert.Equal(LoginCommandHandler.FailureMessage, exception.Message);
        passwords.Verify(service => service.Verify(null, "password"), Times.Once());
    }

    [Fact]
    public async Task HandleAsync_WhenPasswordIsWrong_UsesGenericFailure()
    {
        var agent = new Agent("Alex Turner", "alex@example.test", 10, FixedClock.DefaultNow);
        var agents = new Mock<IAgentRepository>();
        agents.Setup(repository => repository.GetByEmailAsync(agent.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);
        var passwords = new Mock<IPasswordService>();
        passwords.Setup(service => service.Verify(agent, "wrong")).Returns(false);
        var handler = CreateHandler(agents, passwords);

        var exception = await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            handler.HandleAsync(new LoginRequest(agent.Email, "wrong"), CancellationToken.None));

        Assert.Equal(LoginCommandHandler.FailureMessage, exception.Message);
    }

    [Fact]
    public async Task HandleAsync_WhenAgentIsInactive_UsesGenericFailure()
    {
        var agent = new Agent("Alex Turner", "alex@example.test", 10, FixedClock.DefaultNow);
        agent.Deactivate();
        var agents = new Mock<IAgentRepository>();
        agents.Setup(repository => repository.GetByEmailAsync(agent.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);
        var passwords = new Mock<IPasswordService>();
        passwords.Setup(service => service.Verify(agent, "password")).Returns(true);
        var handler = CreateHandler(agents, passwords);

        var exception = await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            handler.HandleAsync(new LoginRequest(agent.Email, "password"), CancellationToken.None));

        Assert.Equal(LoginCommandHandler.FailureMessage, exception.Message);
    }

    private static LoginCommandHandler CreateHandler(
        Mock<IAgentRepository> agents,
        Mock<IPasswordService> passwords) =>
        new(
            agents.Object,
            passwords.Object,
            Mock.Of<ITokenIssuer>(),
            new FixedClock(),
            NullLogger<LoginCommandHandler>.Instance);
}
