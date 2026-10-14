namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>
/// Works out where a ticket stands against its due date. SLA status is always derived from
/// the ticket's dates and the current time - never stored.
/// </summary>
public static class SlaEvaluator
{
    public static SlaStatus Evaluate(
        DateTime createdAtUtc,
        DateTime? dueAtUtc,
        DateTime? resolvedAtUtc,
        DateTime nowUtc,
        int atRiskThresholdPercent)
    {
        if (dueAtUtc is not { } due)
        {
            return SlaStatus.NotApplicable;
        }

        if (resolvedAtUtc is { } resolved)
        {
            return resolved <= due ? SlaStatus.Met : SlaStatus.Breached;
        }

        if (nowUtc > due)
        {
            return SlaStatus.Breached;
        }

        var window = due - createdAtUtc;
        var remaining = due - nowUtc;

        return (decimal)remaining.Ticks * 100 <= (decimal)window.Ticks * atRiskThresholdPercent
            ? SlaStatus.AtRisk
            : SlaStatus.WithinSla;
    }
}
