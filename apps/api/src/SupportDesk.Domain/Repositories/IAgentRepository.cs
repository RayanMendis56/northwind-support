using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Triage;

namespace SupportDesk.Domain.Repositories;

/// <summary>
/// Loads <see cref="Agent"/> aggregates.
/// </summary>
public interface IAgentRepository
{
    Task<Agent?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>
    /// Every agent as the selector sees them. Open tickets exclude <paramref name="excludingTicketId"/>,
    /// so an agent is not penalised for the ticket being re-evaluated.
    /// </summary>
    Task<IReadOnlyList<AgentCandidate>> GetCandidatesAsync(int? excludingTicketId, CancellationToken ct);
}
