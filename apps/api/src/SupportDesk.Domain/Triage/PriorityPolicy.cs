using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Domain.Triage;

public sealed record PriorityDecision(TicketPriority Priority, string Reason);

/// <summary>Decides a new ticket's priority (BR-1). Reads the category's flag, never its name.</summary>
public static class PriorityPolicy
{
    public const TicketPriority DefaultPriority = TicketPriority.Medium;

    public static PriorityDecision Decide(
        TicketPriority? requested, string categoryName, bool categoryForcesCritical)
    {
        if (categoryForcesCritical)
        {
            return new(TicketPriority.Critical, $"Category '{categoryName}' is always treated as Critical");
        }

        return requested is { } priority
            ? new(priority, $"Requested priority {priority} applied")
            : new(DefaultPriority, $"No priority requested, defaulted to {DefaultPriority}");
    }
}
