using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Infrastructure.Services;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Infrastructure.Services;

public sealed class PasswordServiceTests
{
    [Fact]
    public void Verify_ReturnsTrueOnlyForThePasswordThatWasHashed()
    {
        var service = new PasswordService();
        var agent = new Agent("Alex Turner", "alex@example.test", 10, FixedClock.DefaultNow);
        agent.SetPasswordHash(service.Hash(agent, "correct horse battery staple"));

        Assert.True(service.Verify(agent, "correct horse battery staple"));
        Assert.False(service.Verify(agent, "wrong password"));
        Assert.False(service.Verify(null, "wrong password"));
    }

    [Fact]
    public void Verify_ReturnsFalseForAnAgentWithoutAPasswordHash()
    {
        var service = new PasswordService();
        var agent = new Agent("Alex Turner", "alex@example.test", 10, FixedClock.DefaultNow);

        Assert.False(service.Verify(agent, "any password"));
    }

    [Fact]
    public void Hash_IsNotThePlainPasswordAndUsesASaltForEachHash()
    {
        var service = new PasswordService();
        var firstAgent = new Agent("Alex Turner", "alex@example.test", 10, FixedClock.DefaultNow);
        var secondAgent = new Agent("Alex Turner", "alex@example.test", 10, FixedClock.DefaultNow);
        const string password = "correct horse battery staple";

        var firstHash = service.Hash(firstAgent, password);
        var secondHash = service.Hash(secondAgent, password);

        Assert.DoesNotContain(password, firstHash);
        Assert.NotEqual(firstHash, secondHash);
    }
}
