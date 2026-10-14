using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Repositories;
using SupportDesk.Domain.Triage;

namespace SupportDesk.Application.Features.Tickets.Triage;

public sealed record TriageResult(
    TicketPriority Priority,
    DateTime DueAtUtc,
    int? AgentId,
    TriageDto Details);

/// <summary>
/// Combines the pure rules into one decision. Create and escalate both go through here, so the
/// SLA window and the assignment rule each exist exactly once. It holds no rules of its own.
/// </summary>
public sealed class TriageService(SlaCalculator sla, IAgentRepository agents)
{
    /// <summary>BR-1 to BR-5 for a ticket that is being created.</summary>
    public async Task<TriageResult> TriageNewAsync(
        Category category,
        Customer customer,
        TicketPriority? requested,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var priority = PriorityPolicy.Decide(requested, category.Name, category.ForcesCriticalPriority);
        var candidates = await agents.GetCandidatesAsync(excludingTicketId: null, ct);
        var assignment = AgentSelector.Select(candidates, Requirement(category));

        return Build(priority.Priority, priority.Reason, assignment, customer, nowUtc);
    }

    /// <summary>BR-7 for an escalation: new due date, and the current owner kept if still eligible.</summary>
    public async Task<TriageResult> PlanEscalationAsync(
        Ticket ticket,
        Category category,
        Customer customer,
        DateTime nowUtc,
        CancellationToken ct)
    {
        ticket.EnsureCanEscalate();
        var newPriority = ticket.NextPriority!.Value;

        var candidates = await agents.GetCandidatesAsync(excludingTicketId: ticket.Id, ct);
        var requirement = Requirement(category);

        var current = ticket.AssignedAgentId is { } id
            ? candidates.FirstOrDefault(candidate => candidate.AgentId == id)
            : null;

        var assignment = current is not null && AgentSelector.IsEligible(current, requirement)
            ? new AssignmentDecision(current.AgentId, "Current agent is still eligible and keeps the ticket")
            : AgentSelector.Select(candidates, requirement);

        return Build(
            newPriority,
            $"Escalated from {ticket.Priority} to {newPriority}",
            assignment,
            customer,
            nowUtc);
    }

    private TriageResult Build(
        TicketPriority priority,
        string priorityReason,
        AssignmentDecision assignment,
        Customer customer,
        DateTime fromUtc)
    {
        var window = sla.WindowFor(priority, customer.Tier == CustomerTier.Premium);

        return new TriageResult(
            priority,
            fromUtc + window.Duration,
            assignment.AgentId,
            new TriageDto(priority, priorityReason, window.Hours, window.Reason, assignment.Reason));
    }

    private static CategoryRequirement Requirement(Category category) =>
        new(category.Id, category.Name, category.RequiresSpecialist);
}
