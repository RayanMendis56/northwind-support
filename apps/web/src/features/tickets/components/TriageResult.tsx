import { formatDateTime } from '../../../lib/format';
import type { TicketDetail } from '../../../types/api';
import { PriorityBadge } from './TicketBadges';

/** What triage decided for a new ticket, and why. */
export function TriageResult({ ticket }: { ticket: TicketDetail }) {
  const { triage } = ticket;

  if (!triage) {
    return null;
  }

  return (
    <dl className="summary">
      <div>
        <dt>Priority</dt>
        <dd>
          <PriorityBadge priority={triage.appliedPriority} />
          <span className="summary__note">{triage.priorityReason}</span>
        </dd>
      </div>

      <div>
        <dt>Due</dt>
        <dd>
          {formatDateTime(ticket.dueAtUtc)}
          <span className="summary__note">
            {triage.slaWindowHours}h window. {triage.slaReason}
          </span>
        </dd>
      </div>

      <div>
        <dt>Assigned to</dt>
        <dd>
          {ticket.assignedAgent?.fullName ?? 'Unassigned'}
          <span className="summary__note">
            {ticket.assignedAgent ? triage.assignmentReason : `Unassigned. ${triage.assignmentReason}`}
          </span>
        </dd>
      </div>
    </dl>
  );
}
