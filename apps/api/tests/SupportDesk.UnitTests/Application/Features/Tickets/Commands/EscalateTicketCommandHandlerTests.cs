using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Exceptions;
using SupportDesk.Domain.Triage;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Application.Features.Tickets.Commands;

public class EscalateTicketCommandHandlerTests
{
    private const string Reason = "Customer's whole site is down.";
    private const string Actor = "alex.lead";

    private static AgentCandidate Agent(int id, int open, int max = 10, bool active = true) =>
        new(id, active, max, open, new HashSet<int>());

    private static EscalateTicketRequest Request() => new(Reason);

    private static Ticket ExistingTicket(
        TicketCommandTestContext context,
        TicketPriority priority,
        int? agentId = null,
        TicketStatus status = TicketStatus.Open) =>
        context.ExistingTicket(new TicketBuilder()
            .WithPriority(priority)
            .WithStatus(status)
            .AssignedTo(agentId)
            .Build());

    [Fact]
    public async Task HandleAsync_RaisesPriorityOneLevelAndRecomputesTheDueDateFromNow()
    {
        var context = new TicketCommandTestContext();
        var ticket = ExistingTicket(context, TicketPriority.High);
        context.Clock.Advance(TimeSpan.FromHours(5));

        await context.EscalateTicket.HandleAsync(ticket.Id, Request(), Actor, CancellationToken.None);

        Assert.Equal(TicketPriority.Critical, ticket.Priority);
        Assert.Equal(context.Clock.UtcNow.AddHours(2), ticket.DueAtUtc);
        context.VerifySaved(Times.Once());
    }

    [Fact]
    public async Task HandleAsync_KeepsTheCurrentAgentWhenStillEligible_EvenIfAnotherHasFewerTickets()
    {
        var context = new TicketCommandTestContext();
        var ticket = ExistingTicket(context, TicketPriority.Medium, agentId: 3);
        context.WithCandidates(Agent(3, open: 5), Agent(4, open: 0));

        await context.EscalateTicket.HandleAsync(ticket.Id, Request(), Actor, CancellationToken.None);

        Assert.Equal(3, ticket.AssignedAgentId);
    }

    [Fact]
    public async Task HandleAsync_ReassignsWhenTheCurrentAgentIsNoLongerEligible()
    {
        var context = new TicketCommandTestContext();
        var ticket = ExistingTicket(context, TicketPriority.Medium, agentId: 3);
        context.WithCandidates(Agent(3, open: 1, active: false), Agent(4, open: 2));

        await context.EscalateTicket.HandleAsync(ticket.Id, Request(), Actor, CancellationToken.None);

        Assert.Equal(4, ticket.AssignedAgentId);
    }

    [Fact]
    public async Task HandleAsync_WhenNobodyIsEligible_UnassignsTheTicketInsteadOfFailing()
    {
        var context = new TicketCommandTestContext();
        var ticket = ExistingTicket(context, TicketPriority.Medium, agentId: 3);
        context.WithCandidates(Agent(3, open: 5, active: false));

        await context.EscalateTicket.HandleAsync(ticket.Id, Request(), Actor, CancellationToken.None);

        Assert.Null(ticket.AssignedAgentId);
        context.VerifySaved(Times.Once());
    }

    [Fact]
    public async Task HandleAsync_RecordsTheBeforeAndAfterValuesInTheHistoryRow()
    {
        var context = new TicketCommandTestContext();
        var ticket = ExistingTicket(context, TicketPriority.Low, agentId: 3);
        context.WithCandidates(Agent(3, open: 5));

        await context.EscalateTicket.HandleAsync(ticket.Id, Request(), Actor, CancellationToken.None);

        var row = context.AddedEscalation;
        Assert.NotNull(row);
        Assert.Equal(TicketPriority.Low, row.FromPriority);
        Assert.Equal(TicketPriority.Medium, row.ToPriority);
        Assert.Equal(3, row.FromAgentId);
        Assert.Equal(3, row.ToAgentId);
        Assert.Null(row.FromDueAtUtc);
        Assert.Equal(context.Clock.UtcNow.AddHours(12), row.ToDueAtUtc);
        Assert.Equal(Reason, row.Reason);
        Assert.Equal(Actor, row.EscalatedBy);
        Assert.Equal(context.Clock.UtcNow, row.EscalatedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WhenAlreadyCritical_ThrowsAndSavesNothing()
    {
        var context = new TicketCommandTestContext();
        var ticket = ExistingTicket(context, TicketPriority.Critical);

        var error = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => context.EscalateTicket.HandleAsync(ticket.Id, Request(), Actor, CancellationToken.None));

        Assert.Contains("already Critical", error.Message);
        Assert.Null(context.AddedEscalation);
        context.VerifySaved(Times.Never());
    }

    [Theory]
    [InlineData(TicketStatus.Resolved)]
    [InlineData(TicketStatus.Closed)]
    public async Task HandleAsync_WhenResolvedOrClosed_ThrowsAndNamesTheStatus(TicketStatus status)
    {
        var context = new TicketCommandTestContext();
        var ticket = ExistingTicket(context, TicketPriority.Low, status: status);

        var error = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => context.EscalateTicket.HandleAsync(ticket.Id, Request(), Actor, CancellationToken.None));

        Assert.Contains(status.ToString(), error.Message);
        context.VerifySaved(Times.Never());
    }

    [Fact]
    public async Task HandleAsync_WhenTheTicketDoesNotExist_Throws()
    {
        var context = new TicketCommandTestContext();
        context.Tickets
            .Setup(t => t.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => context.EscalateTicket.HandleAsync(999, Request(), Actor, CancellationToken.None));

        context.VerifySaved(Times.Never());
    }
}
