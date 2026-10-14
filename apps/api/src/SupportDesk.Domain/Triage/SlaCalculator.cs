using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Domain.Triage;

public sealed record SlaWindow(TimeSpan Duration, string Reason)
{
    public double Hours => Duration.TotalHours;
}

public sealed class SlaCalculator(SlaOptions options)
{
    public SlaWindow WindowFor(TicketPriority priority, bool isPremiumCustomer)
    {
        var baseHours = options.BaseWindowHours[priority];
        var baseWindow = TimeSpan.FromHours(baseHours);
        var reason = FormattableString.Invariant($"{priority} base window {baseHours:0.##}h");

        if (!isPremiumCustomer)
        {
            return new SlaWindow(baseWindow, reason);
        }

        var reduced = TimeSpan.FromHours(baseHours * options.PremiumCustomerMultiplier);
        var floor = TimeSpan.FromHours(options.MinimumWindowHours);

        return reduced < floor
            ? new SlaWindow(floor, FormattableString.Invariant(
                $"{reason}, reduced for a Premium customer but not below the {options.MinimumWindowHours:0.##}h minimum"))
            : new SlaWindow(reduced, FormattableString.Invariant(
                $"{reason}, reduced for a Premium customer (x{options.PremiumCustomerMultiplier:0.##})"));
    }

    public DateTime DueAt(DateTime fromUtc, TicketPriority priority, bool isPremiumCustomer) =>
        fromUtc + WindowFor(priority, isPremiumCustomer).Duration;
}
