using Microsoft.Extensions.Logging;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Tickets.Queries.GetTicket;
using SupportDesk.Application.Features.Tickets.Triage;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;

public sealed class EscalateTicketCommandHandler(
    ITicketRepository tickets,
    ICustomerRepository customers,
    ICategoryRepository categories,
    TriageService triage,
    ITicketQueries ticketQueries,
    IUnitOfWork unitOfWork,
    IClock clock,
    GetTicketQueryHandler getTicket,
    ILogger<EscalateTicketCommandHandler> logger)
{
    /// <exception cref="NotFoundException">The ticket does not exist.</exception>
    /// <exception cref="SupportDesk.Domain.Exceptions.BusinessRuleViolationException">
    /// The ticket is Critical, Resolved or Closed.
    /// </exception>
    public async Task<EscalateTicketResponse> HandleAsync(
        int id,
        EscalateTicketRequest request,
        string actor,
        CancellationToken ct)
    {
        var ticket = await tickets.GetByIdAsync(id, ct) ?? throw new NotFoundException("Ticket", id);
        var category = await categories.GetByIdAsync(ticket.CategoryId, ct)
            ?? throw new NotFoundException("Category", ticket.CategoryId);
        var customer = await customers.GetByIdAsync(ticket.CustomerId, ct)
            ?? throw new NotFoundException("Customer", ticket.CustomerId);

        var now = clock.UtcNow;
        var plan = await triage.PlanEscalationAsync(ticket, category, customer, now, ct);
        var escalation = ticket.Escalate(request.Reason, actor, plan.DueAtUtc, plan.AgentId, now);

        await tickets.AddEscalationAsync(escalation, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Ticket {Reference} escalated to {Priority} by {Actor}.",
            ticket.Reference,
            ticket.Priority,
            actor);

        var updated = await getTicket.HandleAsync(id, ct);
        updated.Triage = plan.Details;

        var history = await ticketQueries.GetEscalationsAsync(id, ct);
        return new EscalateTicketResponse(updated, history.First(entry => entry.Id == escalation.Id));
    }
}
