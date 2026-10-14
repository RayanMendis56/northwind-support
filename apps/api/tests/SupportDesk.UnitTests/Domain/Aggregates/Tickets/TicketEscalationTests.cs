using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Exceptions;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Domain.Aggregates.Tickets;

public sealed class TicketEscalationTests
{
    private static readonly DateTime Now = FixedClock.DefaultNow;

    [Fact]
    public void Escalate_RaisesPriorityAndReturnsBeforeAndAfterHistory()
    {
        var ticket = new TicketBuilder()
            .WithReference("TCK-0042")
            .WithPriority(TicketPriority.Medium)
            .AssignedTo(7)
            .Build();
        var previousDueAtUtc = Now.AddHours(24);
        var nextDueAtUtc = Now.AddHours(5);
        ticket.SetDueDate(previousDueAtUtc, Now);

        var escalation = ticket.Escalate(
            "  Customer impact increased  ",
            "  Team Lead  ",
            nextDueAtUtc,
            newAgentId: 9,
            Now.AddMinutes(10));

        Assert.Equal(TicketPriority.High, ticket.Priority);
        Assert.Equal(9, ticket.AssignedAgentId);
        Assert.Equal(nextDueAtUtc, ticket.DueAtUtc);
        Assert.Equal(Now.AddMinutes(10), ticket.UpdatedAtUtc);
        Assert.Equal(TicketPriority.Medium, escalation.FromPriority);
        Assert.Equal(TicketPriority.High, escalation.ToPriority);
        Assert.Equal(7, escalation.FromAgentId);
        Assert.Equal(9, escalation.ToAgentId);
        Assert.Equal(previousDueAtUtc, escalation.FromDueAtUtc);
        Assert.Equal(nextDueAtUtc, escalation.ToDueAtUtc);
        Assert.Equal("Customer impact increased", escalation.Reason);
        Assert.Equal("Team Lead", escalation.EscalatedBy);
        Assert.Equal(Now.AddMinutes(10), escalation.EscalatedAtUtc);
    }

    [Theory]
    [InlineData(TicketPriority.Low, TicketPriority.Medium)]
    [InlineData(TicketPriority.Medium, TicketPriority.High)]
    [InlineData(TicketPriority.High, TicketPriority.Critical)]
    public void Escalate_RaisesThePriorityByExactlyOneLevel(
        TicketPriority fromPriority,
        TicketPriority expectedPriority)
    {
        var ticket = new TicketBuilder().WithPriority(fromPriority).Build();

        var history = ticket.Escalate("Impact increased", "Lead", Now.AddHours(10), null, Now);

        Assert.Equal(expectedPriority, ticket.Priority);
        Assert.Equal(fromPriority, history.FromPriority);
        Assert.Equal(expectedPriority, history.ToPriority);
    }

    [Fact]
    public void Escalate_WhenCriticalThrowsAndChangesNothing()
    {
        var ticket = new TicketBuilder().WithPriority(TicketPriority.Critical).Build();

        var exception = Assert.Throws<BusinessRuleViolationException>(
            () => ticket.Escalate("Impact increased", "Lead", Now.AddHours(1), null, Now));

        Assert.Contains("Critical", exception.Message);
        Assert.Equal(TicketPriority.Critical, ticket.Priority);
    }

    [Theory]
    [InlineData(TicketStatus.Resolved)]
    [InlineData(TicketStatus.Closed)]
    public void Escalate_WhenTicketIsTerminalThrowsBusinessRuleViolation(TicketStatus status)
    {
        var ticket = new TicketBuilder().WithStatus(status).Build();

        var exception = Assert.Throws<BusinessRuleViolationException>(
            () => ticket.Escalate("Impact increased", "Lead", Now.AddHours(1), null, Now));

        Assert.Contains(status.ToString(), exception.Message);
        Assert.Equal(TicketPriority.Medium, ticket.Priority);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Escalate_WithoutAReasonThrowsArgumentException(string? reason)
    {
        var ticket = new TicketBuilder().Build();

        Assert.ThrowsAny<ArgumentException>(
            () => ticket.Escalate(reason!, "Lead", Now.AddHours(1), null, Now));
        Assert.Equal(TicketPriority.Medium, ticket.Priority);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Escalate_WithoutAnActorThrowsArgumentException(string? escalatedBy)
    {
        var ticket = new TicketBuilder().Build();

        Assert.ThrowsAny<ArgumentException>(
            () => ticket.Escalate("Impact increased", escalatedBy!, Now.AddHours(1), null, Now));
        Assert.Equal(TicketPriority.Medium, ticket.Priority);
    }
}
