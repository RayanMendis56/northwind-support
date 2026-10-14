using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Triage;
using SupportDesk.Infrastructure.Data;
using SupportDesk.Infrastructure.Queries;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Infrastructure.Queries;

public sealed class TicketQueriesTests
{
    [Fact]
    public async Task GetPagedAsync_FiltersPriorityByExactMatch()
    {
        await using var fixture = await QueryFixture.CreateAsync();

        var result = await fixture.Queries.GetPagedAsync(
            new TicketQuery { Priority = TicketPriority.High },
            CancellationToken.None);

        Assert.Equal(6, result.TotalCount);
        Assert.All(result.Items, ticket => Assert.Equal(TicketPriority.High, ticket.Priority));
    }

    [Fact]
    public async Task GetPagedAsync_CombinesStatusCategoryAndCustomerFilters()
    {
        await using var fixture = await QueryFixture.CreateAsync();

        var result = await fixture.Queries.GetPagedAsync(
            new TicketQuery
            {
                Status = TicketStatus.InProgress,
                CategoryId = fixture.BillingCategoryId,
                CustomerId = fixture.ContosoCustomerId
            },
            CancellationToken.None);

        var ticket = Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("TCK-1007", ticket.Reference);
        Assert.Equal(TicketStatus.InProgress, ticket.Status);
        Assert.Equal("Billing", ticket.Category.Name);
        Assert.Equal("Contoso Ltd", ticket.Customer.Name);
    }

    [Fact]
    public async Task GetPagedAsync_SearchesTrimmedCustomerName()
    {
        await using var fixture = await QueryFixture.CreateAsync();

        var result = await fixture.Queries.GetPagedAsync(
            new TicketQuery { Search = " contoso " },
            CancellationToken.None);

        Assert.Equal(5, result.TotalCount);
        Assert.All(result.Items, ticket =>
        {
            Assert.Equal("Contoso Ltd", ticket.Customer.Name);
            Assert.DoesNotContain("Contoso", ticket.Title);
        });
    }

    [Fact]
    public async Task GetPagedAsync_CountsAndPagesFilteredRows()
    {
        await using var fixture = await QueryFixture.CreateAsync();

        var result = await fixture.Queries.GetPagedAsync(
            new TicketQuery { Priority = TicketPriority.High, PageSize = 2 },
            CancellationToken.None);

        Assert.Equal(6, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, ticket => Assert.Equal(TicketPriority.High, ticket.Priority));
    }

    [Fact]
    public async Task GetPagedAsync_UnassignedOnlyReturnsTicketsWithoutAnAgent()
    {
        await using var fixture = await QueryFixture.CreateAsync();

        var result = await fixture.Queries.GetPagedAsync(
            new TicketQuery { UnassignedOnly = true },
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, ticket => Assert.Null(ticket.AssignedAgent));
    }

    [Fact]
    public async Task ExistsAsync_ReportsWhetherTheTicketExists()
    {
        await using var fixture = await QueryFixture.CreateAsync();

        Assert.True(await fixture.Queries.ExistsAsync(fixture.TicketId, CancellationToken.None));
        Assert.False(await fixture.Queries.ExistsAsync(-1, CancellationToken.None));
    }

    [Fact]
    public async Task GetEscalationsAsync_ReturnsHistoryWithAgentNamesNewestFirst()
    {
        await using var fixture = await QueryFixture.CreateAsync();
        var ticket = await fixture.Db.Set<Ticket>().SingleAsync(t => t.Id == fixture.TicketId);
        var escalatedAt = FixedClock.DefaultNow.AddMinutes(5);
        var record = ticket.Escalate(
            "Customer impact increased",
            "team.lead",
            escalatedAt.AddHours(4),
            newAgentId: null,
            escalatedAt);
        await fixture.Db.Set<TicketEscalation>().AddAsync(record);
        await fixture.Db.SaveChangesAsync();

        var history = await fixture.Queries.GetEscalationsAsync(fixture.TicketId, CancellationToken.None);

        var escalation = Assert.Single(history);
        Assert.Equal(TicketPriority.High, escalation.FromPriority);
        Assert.Equal(TicketPriority.Critical, escalation.ToPriority);
        Assert.Equal("Alex Turner", escalation.FromAgent?.FullName);
        Assert.Null(escalation.ToAgent);
        Assert.Equal("team.lead", escalation.EscalatedBy);
    }

    private sealed class QueryFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private QueryFixture(SqliteConnection connection, SupportDbContext db)
        {
            _connection = connection;
            Db = db;
            Queries = new TicketQueries(db, new FixedClock(), new SlaOptions
            {
                AtRiskThresholdPercent = 25
            });
        }

        public SupportDbContext Db { get; }

        public TicketQueries Queries { get; }

        public int ContosoCustomerId { get; private set; }

        public int BillingCategoryId { get; private set; }

        public int TicketId { get; private set; }

        public static async Task<QueryFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<SupportDbContext>()
                .UseSqlite(connection)
                .Options;
            var db = new SupportDbContext(options);
            var fixture = new QueryFixture(connection, db);

            await db.Database.EnsureCreatedAsync();
            await fixture.SeedAsync();
            return fixture;
        }

        private async Task SeedAsync()
        {
            var now = FixedClock.DefaultNow;
            var contoso = new Customer("Contoso Ltd", "support@contoso.example", null, CustomerTier.Premium, now);
            var fabrikam = new Customer("Fabrikam Inc", "support@fabrikam.example", null, CustomerTier.Standard, now);
            var general = new Category("General", requiresSpecialist: false, forcesCriticalPriority: false);
            var billing = new Category("Billing", requiresSpecialist: true, forcesCriticalPriority: false);
            var agent = new Agent("Alex Turner", "alex@example.test", maxOpenTickets: 10, now);

            Db.AddRange(contoso, fabrikam, general, billing, agent);
            await Db.SaveChangesAsync();

            ContosoCustomerId = contoso.Id;
            BillingCategoryId = billing.Id;

            var tickets = new[]
            {
                CreateTicket("TCK-1001", "High assigned", contoso.Id, general.Id, TicketPriority.High, TicketStatus.InProgress, agent.Id, now),
                CreateTicket("TCK-1002", "High unassigned", contoso.Id, general.Id, TicketPriority.High, TicketStatus.New, null, now),
                CreateTicket("TCK-1003", "Medium unassigned", contoso.Id, general.Id, TicketPriority.Medium, TicketStatus.New, null, now),
                CreateTicket("TCK-1004", "Low priority", fabrikam.Id, general.Id, TicketPriority.Low, TicketStatus.New, agent.Id, now),
                CreateTicket("TCK-1005", "Critical priority", fabrikam.Id, general.Id, TicketPriority.Critical, TicketStatus.New, agent.Id, now),
                CreateTicket("TCK-1006", "High open", fabrikam.Id, general.Id, TicketPriority.High, TicketStatus.Open, agent.Id, now),
                CreateTicket("TCK-1007", "High billing in progress", contoso.Id, billing.Id, TicketPriority.High, TicketStatus.InProgress, agent.Id, now),
                CreateTicket("TCK-1008", "High billing other customer", fabrikam.Id, billing.Id, TicketPriority.High, TicketStatus.InProgress, agent.Id, now),
                CreateTicket("TCK-1009", "High general open", contoso.Id, general.Id, TicketPriority.High, TicketStatus.Open, agent.Id, now)
            };

            Db.AddRange(tickets);
            await Db.SaveChangesAsync();
            TicketId = tickets[0].Id;
        }

        private static Ticket CreateTicket(
            string reference,
            string title,
            int customerId,
            int categoryId,
            TicketPriority priority,
            TicketStatus status,
            int? assignedAgentId,
            DateTime now)
        {
            var ticket = Ticket.Raise(
                reference,
                title,
                "Description",
                customerId,
                categoryId,
                priority,
                now);

            if (assignedAgentId is not null)
            {
                ticket.AssignTo(assignedAgentId, now);
            }

            if (status != TicketStatus.New)
            {
                ticket.ChangeStatus(status, now);
            }

            return ticket;
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
