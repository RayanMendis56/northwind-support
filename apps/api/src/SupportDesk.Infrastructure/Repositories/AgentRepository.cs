using Microsoft.EntityFrameworkCore;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Repositories;
using SupportDesk.Domain.Triage;
using SupportDesk.Infrastructure.Data;

namespace SupportDesk.Infrastructure.Repositories;

public sealed class AgentRepository(SupportDbContext db) : IAgentRepository
{
    /// <summary>Loads the whole aggregate, specializations included.</summary>
    public Task<Agent?> GetByIdAsync(int id, CancellationToken ct) =>
        db.Set<Agent>()
            .Include(a => a.Specializations)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<AgentCandidate>> GetCandidatesAsync(
        int? excludingTicketId,
        CancellationToken ct)
    {
        var excluded = excludingTicketId ?? 0;

        var openCounts = await db.Set<Ticket>()
            .AsNoTracking()
            .Where(t => t.AssignedAgentId != null
                        && t.Id != excluded
                        && t.Status != TicketStatus.Resolved
                        && t.Status != TicketStatus.Closed)
            .GroupBy(t => t.AssignedAgentId!.Value)
            .Select(g => new { AgentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.AgentId, x => x.Count, ct);

        var agents = await db.Set<Agent>()
            .AsNoTracking()
            .Select(a => new
            {
                a.Id,
                a.IsActive,
                a.MaxOpenTickets,
                CategoryIds = a.Specializations.Select(s => s.CategoryId).ToList()
            })
            .ToListAsync(ct);

        return agents
            .Select(a => new AgentCandidate(
                a.Id,
                a.IsActive,
                a.MaxOpenTickets,
                openCounts.GetValueOrDefault(a.Id),
                a.CategoryIds.ToHashSet()))
            .ToList();
    }
}
