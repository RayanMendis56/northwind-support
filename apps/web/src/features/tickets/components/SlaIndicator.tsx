import { Badge } from '../../../components/Badge';
import { formatDateTime } from '../../../lib/format';
import type { SlaStatus } from '../../../types/api';
import { slaPresentation } from './slaStatus';

interface SlaIndicatorProps {
  slaStatus: SlaStatus;
  /** When given (including null), the due date is shown beside the status. */
  dueAtUtc?: string | null;
}

/** Where a ticket stands against its SLA. Used by the list and the detail page. */
export function SlaIndicator({ slaStatus, dueAtUtc }: SlaIndicatorProps) {
  const { text, symbol, tone } = slaPresentation[slaStatus];

  return (
    <div className="sla-cell">
      <Badge tone={tone}>
        <span aria-hidden="true">{symbol}</span> <span>{text}</span>
      </Badge>
      {dueAtUtc !== undefined && (
        <span className="sla-cell__due">{formatDateTime(dueAtUtc)}</span>
      )}
    </div>
  );
}
