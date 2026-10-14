using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Contracts.Tickets;

/// <summary>Body of POST /api/tickets/{id}/escalate. EscalatedBy is replaced by the token in Task 3.</summary>
public sealed record EscalateTicketRequest(string Reason, string? EscalatedBy);

/// <summary>Why a ticket got the priority, due date and owner it did.</summary>
public sealed record TriageDto(
    TicketPriority AppliedPriority,
    string PriorityReason,
    double SlaWindowHours,
    string SlaReason,
    string AssignmentReason);

public sealed record EscalationDto(
    int Id,
    TicketPriority FromPriority,
    TicketPriority ToPriority,
    AgentSummaryDto? FromAgent,
    AgentSummaryDto? ToAgent,
    DateTime? FromDueAtUtc,
    DateTime ToDueAtUtc,
    string Reason,
    string EscalatedBy,
    DateTime EscalatedAtUtc);

/// <summary>Response of POST /api/tickets/{id}/escalate: the updated ticket and the row that was recorded.</summary>
public sealed record EscalateTicketResponse(TicketDetailDto Ticket, EscalationDto Escalation);
