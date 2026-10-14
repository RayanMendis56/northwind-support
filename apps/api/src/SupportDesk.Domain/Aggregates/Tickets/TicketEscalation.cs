using SupportDesk.Domain.Common;

namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>
/// An immutable record of one escalation. It has no public way to be changed, and nothing
/// updates or deletes it.
/// </summary>
public sealed class TicketEscalation : Entity
{
    private TicketEscalation()
    {
        // For EF Core.
    }

    public int TicketId { get; private set; }

    public TicketPriority FromPriority { get; private set; }

    public TicketPriority ToPriority { get; private set; }

    public int? FromAgentId { get; private set; }

    public int? ToAgentId { get; private set; }

    public DateTime? FromDueAtUtc { get; private set; }

    public DateTime ToDueAtUtc { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public string EscalatedBy { get; private set; } = string.Empty;

    public DateTime EscalatedAtUtc { get; private set; }

    internal static TicketEscalation Record(
        int ticketId,
        TicketPriority fromPriority,
        TicketPriority toPriority,
        int? fromAgentId,
        int? toAgentId,
        DateTime? fromDueAtUtc,
        DateTime toDueAtUtc,
        string reason,
        string escalatedBy,
        DateTime escalatedAtUtc) => new()
    {
        TicketId = ticketId,
        FromPriority = fromPriority,
        ToPriority = toPriority,
        FromAgentId = fromAgentId,
        ToAgentId = toAgentId,
        FromDueAtUtc = fromDueAtUtc,
        ToDueAtUtc = toDueAtUtc,
        Reason = reason.Trim(),
        EscalatedBy = escalatedBy.Trim(),
        EscalatedAtUtc = escalatedAtUtc
    };
}
