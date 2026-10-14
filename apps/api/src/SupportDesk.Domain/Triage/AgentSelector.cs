namespace SupportDesk.Domain.Triage;

/// <summary>An agent as the selector sees them: no entity, no database.</summary>
public sealed record AgentCandidate(
    int AgentId,
    bool IsActive,
    int MaxOpenTickets,
    int OpenTickets,
    IReadOnlySet<int> SpecializationCategoryIds);

public sealed record CategoryRequirement(int CategoryId, string CategoryName, bool RequiresSpecialist);

public sealed record AssignmentDecision(int? AgentId, string Reason);

public static class AgentSelector
{
    public static bool IsEligible(AgentCandidate agent, CategoryRequirement category) =>
        agent.IsActive
        && agent.OpenTickets < agent.MaxOpenTickets
        && (!category.RequiresSpecialist || agent.SpecializationCategoryIds.Contains(category.CategoryId));

    public static AssignmentDecision Select(IEnumerable<AgentCandidate> agents, CategoryRequirement category)
    {
        var chosen = agents
            .Where(agent => IsEligible(agent, category))
            .OrderBy(agent => agent.OpenTickets)
            .ThenBy(agent => agent.AgentId)
            .FirstOrDefault();

        if (chosen is not null)
        {
            return new AssignmentDecision(chosen.AgentId, category.RequiresSpecialist
                ? $"Fewest open tickets among {category.CategoryName} specialists"
                : "Fewest open tickets among available agents");
        }

        return new AssignmentDecision(null, category.RequiresSpecialist
            ? $"No available agent specialised in {category.CategoryName}"
            : "No available agent with capacity");
    }
}
