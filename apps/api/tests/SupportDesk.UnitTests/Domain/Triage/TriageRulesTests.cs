using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Triage;

namespace SupportDesk.UnitTests.Domain.Triage;

public sealed class SlaCalculatorRulesTests
{
    [Theory]
    [InlineData(TicketPriority.Critical, false, 4)]
    [InlineData(TicketPriority.High, false, 8)]
    [InlineData(TicketPriority.Medium, false, 24)]
    [InlineData(TicketPriority.Low, false, 72)]
    [InlineData(TicketPriority.Critical, true, 2)]
    [InlineData(TicketPriority.High, true, 4)]
    [InlineData(TicketPriority.Medium, true, 12)]
    [InlineData(TicketPriority.Low, true, 36)]
    public void WindowFor_AppliesBaseWindowAndPremiumReduction(
        TicketPriority priority,
        bool premium,
        double hours)
    {
        var window = new SlaCalculator(Options()).WindowFor(priority, premium);

        Assert.Equal(TimeSpan.FromHours(hours), window.Duration);
    }

    [Fact]
    public void WindowFor_PremiumNeverDropsBelowTheFloor()
    {
        var options = Options();
        options.BaseWindowHours[TicketPriority.Critical] = 1;

        var window = new SlaCalculator(options).WindowFor(TicketPriority.Critical, isPremiumCustomer: true);

        Assert.Equal(TimeSpan.FromHours(1), window.Duration);
        Assert.Contains("minimum", window.Reason);
    }

    [Fact]
    public void DueAt_IsMeasuredFromTheGivenMoment()
    {
        var from = new DateTime(2026, 3, 2, 14, 0, 0, DateTimeKind.Utc);

        var due = new SlaCalculator(Options()).DueAt(from, TicketPriority.Critical, isPremiumCustomer: false);

        Assert.Equal(new DateTime(2026, 3, 2, 18, 0, 0, DateTimeKind.Utc), due);
    }

    private static SlaOptions Options(double floor = 1) => new()
    {
        BaseWindowHours = new()
        {
            [TicketPriority.Low] = 72,
            [TicketPriority.Medium] = 24,
            [TicketPriority.High] = 8,
            [TicketPriority.Critical] = 4
        },
        PremiumCustomerMultiplier = 0.5,
        MinimumWindowHours = floor,
        AtRiskThresholdPercent = 25
    };
}

public sealed class PriorityPolicyRulesTests
{
    [Theory]
    [InlineData(TicketPriority.Low)]
    [InlineData(null)]
    public void Decide_WhenCategoryForcesCritical_IsAlwaysCritical(TicketPriority? requested)
    {
        var decision = PriorityPolicy.Decide(requested, "Security", categoryForcesCritical: true);

        Assert.Equal(TicketPriority.Critical, decision.Priority);
        Assert.Contains("Security", decision.Reason);
    }

    [Fact]
    public void Decide_RespectsTheRequestedPriority() =>
        Assert.Equal(TicketPriority.High, PriorityPolicy.Decide(TicketPriority.High, "Billing", false).Priority);

    [Fact]
    public void Decide_WithoutARequest_DefaultsToMedium() =>
        Assert.Equal(TicketPriority.Medium, PriorityPolicy.Decide(null, "Technical", false).Priority);
}

public sealed class AgentSelectorRulesTests
{
    private const int BillingCategoryId = 2;
    private const int TechnicalCategoryId = 3;
    private static readonly CategoryRequirement Billing = new(BillingCategoryId, "Billing", true);
    private static readonly CategoryRequirement Technical = new(TechnicalCategoryId, "Technical", false);

    [Fact]
    public void Select_PicksTheFewestOpenTickets()
    {
        var result = AgentSelector.Select([Agent(1, 5), Agent(2, 2), Agent(3, 4)], Technical);

        Assert.Equal(2, result.AgentId);
    }

    [Fact]
    public void Select_WhenSpecialistIsRequired_IgnoresNonSpecialists()
    {
        var result = AgentSelector.Select(
            [Agent(1, 0), Agent(2, 6, 10, true, BillingCategoryId)], Billing);

        Assert.Equal(2, result.AgentId);
        Assert.Equal("Fewest open tickets among Billing specialists", result.Reason);
    }

    [Fact]
    public void Select_SkipsAgentsAtTheirLimitButAllowsOneBelowIt()
    {
        var full = AgentSelector.Select([Agent(1, 6, 6)], Technical);
        var oneFree = AgentSelector.Select([Agent(1, 5, 6)], Technical);

        Assert.Null(full.AgentId);
        Assert.Equal(1, oneFree.AgentId);
    }

    [Fact]
    public void Select_SkipsInactiveAgents() =>
        Assert.Null(AgentSelector.Select([Agent(1, 0, 8, active: false)], Technical).AgentId);

    [Fact]
    public void Select_BreaksTiesByLowestId()
    {
        var result = AgentSelector.Select([Agent(7, 3), Agent(4, 3), Agent(9, 3)], Technical);

        Assert.Equal(4, result.AgentId);
    }

    [Fact]
    public void Select_WhenNobodyIsEligible_ReturnsUnassignedWithAReason()
    {
        var result = AgentSelector.Select([Agent(1, 0)], Billing);

        Assert.Null(result.AgentId);
        Assert.Equal("No available agent specialised in Billing", result.Reason);
    }

    [Fact]
    public void IsEligibleRequiresCapacityAndSpecialization()
    {
        Assert.True(AgentSelector.IsEligible(Agent(1, 1, 10, true, BillingCategoryId), Billing));
        Assert.False(AgentSelector.IsEligible(Agent(2, 10, 10, true, BillingCategoryId), Billing));
        Assert.False(AgentSelector.IsEligible(Agent(3, 0), Billing));
    }

    private static AgentCandidate Agent(
        int id,
        int open,
        int max = 10,
        bool active = true,
        params int[] skills) =>
        new(id, active, max, open, skills.ToHashSet());
}

public sealed class SlaEvaluatorRulesTests
{
    private static readonly DateTime Created = new(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Due = Created.AddHours(24);
    private static readonly DateTime AtRiskStart = Due.AddHours(-6);
    private const int Threshold = 25;

    [Fact]
    public void NoDueDate_IsNotApplicable() =>
        Assert.Equal(SlaStatus.NotApplicable,
            SlaEvaluator.Evaluate(Created, null, null, Created, Threshold));

    [Fact]
    public void ExactlyOnDueDate_IsAtRisk() =>
        Assert.Equal(SlaStatus.AtRisk, Evaluate(Due));

    [Fact]
    public void OneTickAfterDueDate_IsBreached() =>
        Assert.Equal(SlaStatus.Breached, Evaluate(Due.AddTicks(1)));

    [Fact]
    public void ExactlyAtThreshold_IsAtRisk() =>
        Assert.Equal(SlaStatus.AtRisk, Evaluate(AtRiskStart));

    [Fact]
    public void OneTickBeforeThreshold_IsWithinSla() =>
        Assert.Equal(SlaStatus.WithinSla, Evaluate(AtRiskStart.AddTicks(-1)));

    [Fact]
    public void ResolvedExactlyOnDueDate_IsMet() =>
        Assert.Equal(SlaStatus.Met, Evaluate(Due.AddDays(5), resolved: Due));

    [Fact]
    public void ResolvedOneTickLate_IsBreached() =>
        Assert.Equal(SlaStatus.Breached, Evaluate(Due, resolved: Due.AddTicks(1)));

    [Fact]
    public void ConfiguredThresholdIsUsed()
    {
        Assert.Equal(SlaStatus.WithinSla,
            SlaEvaluator.Evaluate(Created, Due, null, AtRiskStart, atRiskThresholdPercent: 20));
    }

    private static SlaStatus Evaluate(DateTime now, DateTime? resolved = null) =>
        SlaEvaluator.Evaluate(Created, Due, resolved, now, Threshold);
}
