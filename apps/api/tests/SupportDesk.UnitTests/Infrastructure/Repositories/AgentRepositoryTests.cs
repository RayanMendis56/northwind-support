using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Infrastructure.Data;
using SupportDesk.Infrastructure.Repositories;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Infrastructure.Repositories;

public sealed class AgentRepositoryTests
{
    [Fact]
    public async Task GetCandidatesAsync_CountsOpenTicketsAndExcludesTheCurrentTicket()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SupportDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new SupportDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var now = FixedClock.DefaultNow;
        var category = new Category("Outage", requiresSpecialist: true, forcesCriticalPriority: true);
        var customer = new Customer("Contoso Ltd", "contoso@example.test", null, CustomerTier.Premium, now);
        db.AddRange(category, customer);
        await db.SaveChangesAsync();

        var assigned = new Agent("Ben Osei", "ben@example.test", maxOpenTickets: 8, now);
        assigned.AddSpecialization(category.Id);
        var unassigned = new Agent("Priya Nair", "priya@example.test", maxOpenTickets: 8, now);

        db.AddRange(assigned, unassigned);
        await db.SaveChangesAsync();

        var current = CreateTicket("TCK-2001", customer.Id, category.Id, assigned.Id, TicketStatus.InProgress, now);
        var otherOpen = CreateTicket("TCK-2002", customer.Id, category.Id, assigned.Id, TicketStatus.New, now);
        var resolved = CreateTicket("TCK-2003", customer.Id, category.Id, assigned.Id, TicketStatus.Resolved, now);
        var closed = CreateTicket("TCK-2004", customer.Id, category.Id, assigned.Id, TicketStatus.Closed, now);
        db.AddRange(current, otherOpen, resolved, closed);
        await db.SaveChangesAsync();

        var repository = new AgentRepository(db);

        var all = await repository.GetCandidatesAsync(excludingTicketId: null, CancellationToken.None);
        var excludingCurrent = await repository.GetCandidatesAsync(current.Id, CancellationToken.None);

        var allAssigned = Assert.Single(all, agent => agent.AgentId == assigned.Id);
        var excludingAssigned = Assert.Single(excludingCurrent, agent => agent.AgentId == assigned.Id);
        var empty = Assert.Single(excludingCurrent, agent => agent.AgentId == unassigned.Id);

        Assert.Equal(2, allAssigned.OpenTickets);
        Assert.Equal(1, excludingAssigned.OpenTickets);
        Assert.Equal(0, empty.OpenTickets);
        Assert.Contains(category.Id, allAssigned.SpecializationCategoryIds);
    }

    private static Ticket CreateTicket(
        string reference,
        int customerId,
        int categoryId,
        int agentId,
        TicketStatus status,
        DateTime now)
    {
        var ticket = Ticket.Raise(
            reference,
            "Service outage",
            "A description long enough for a realistic test ticket.",
            customerId,
            categoryId,
            TicketPriority.High,
            now);
        ticket.AssignTo(agentId, now);

        if (status != TicketStatus.New)
        {
            ticket.ChangeStatus(status, now);
        }

        return ticket;
    }
}
