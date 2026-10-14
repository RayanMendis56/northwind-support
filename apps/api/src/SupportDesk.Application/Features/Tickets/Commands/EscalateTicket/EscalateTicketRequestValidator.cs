using FluentValidation;
using SupportDesk.Application.Contracts.Tickets;

namespace SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;

public sealed class EscalateTicketRequestValidator : AbstractValidator<EscalateTicketRequest>
{
    public const int MinReasonLength = 5;
    public const int MaxReasonLength = 500;

    public EscalateTicketRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .Must(reason => reason is null || reason.Trim().Length is >= MinReasonLength and <= MaxReasonLength)
            .WithMessage($"Reason must be between {MinReasonLength} and {MaxReasonLength} characters.");
    }
}
