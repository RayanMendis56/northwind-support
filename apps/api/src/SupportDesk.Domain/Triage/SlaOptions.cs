using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Domain.Triage;

/// <summary>SLA rules that can change without touching code. Bound from the "Sla" config section.</summary>
public sealed class SlaOptions
{
    public const string SectionName = "Sla";

    public Dictionary<TicketPriority, double> BaseWindowHours { get; set; } = [];

    public double PremiumCustomerMultiplier { get; set; }

    public double MinimumWindowHours { get; set; }

    public int AtRiskThresholdPercent { get; set; }

    /// <summary>Fails at startup rather than on the first ticket if the configuration is unusable.</summary>
    public void Validate()
    {
        foreach (var priority in Enum.GetValues<TicketPriority>())
        {
            if (!BaseWindowHours.TryGetValue(priority, out var hours) || hours <= 0)
            {
                throw new InvalidOperationException(
                    $"Sla:BaseWindowHours:{priority} must be a positive number of hours.");
            }
        }

        if (PremiumCustomerMultiplier is <= 0 or > 1)
        {
            throw new InvalidOperationException(
                "Sla:PremiumCustomerMultiplier must be above 0 and at most 1.");
        }

        if (MinimumWindowHours < 0)
        {
            throw new InvalidOperationException("Sla:MinimumWindowHours cannot be negative.");
        }

        if (AtRiskThresholdPercent is < 0 or > 100)
        {
            throw new InvalidOperationException(
                "Sla:AtRiskThresholdPercent must be between 0 and 100.");
        }
    }
}
