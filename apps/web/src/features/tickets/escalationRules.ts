import type { TicketDetail } from '../../types/api';
import { ESCALATION_REASON_MAX, ESCALATION_REASON_MIN } from '../../types/api';

/** Mirrors EscalateTicketRequestValidator: the length is measured after trimming. */
export function validateReason(reason: string): string | undefined {
  const length = reason.trim().length;

  if (length < ESCALATION_REASON_MIN) {
    return `Give a reason of at least ${ESCALATION_REASON_MIN} characters.`;
  }

  if (length > ESCALATION_REASON_MAX) {
    return `Keep the reason to ${ESCALATION_REASON_MAX} characters or fewer.`;
  }

  return undefined;
}

/** Mirrors Ticket.EnsureCanEscalate. Returns why the ticket cannot be escalated, or undefined. */
export function escalationBlockedReason(
  ticket: Pick<TicketDetail, 'priority' | 'status'>,
): string | undefined {
  if (ticket.status === 'Resolved' || ticket.status === 'Closed') {
    return `This ticket is ${ticket.status} and cannot be escalated.`;
  }

  if (ticket.priority === 'Critical') {
    return 'This ticket is already Critical and cannot be escalated further.';
  }

  return undefined;
}
