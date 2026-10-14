import { EmptyState } from '../../../components/EmptyState';
import { ErrorMessage } from '../../../components/ErrorMessage';
import { Spinner } from '../../../components/Spinner';
import { formatDateTime } from '../../../lib/format';
import type { AgentSummary, Escalation } from '../../../types/api';

interface EscalationHistoryProps {
  escalations?: Escalation[];
  isLoading: boolean;
  error?: string;
  onRetry: () => void;
}

export function EscalationHistory({
  escalations,
  isLoading,
  error,
  onRetry,
}: EscalationHistoryProps) {
  return (
    <div className="card">
      <h2>Escalation history</h2>

      {error && <ErrorMessage message={error} onRetry={onRetry} />}
      {isLoading && !escalations && <Spinner label="Loading escalation history" />}

      {escalations && escalations.length === 0 && (
        <EmptyState title="No escalations yet" description="Escalations will be listed here." />
      )}

      {escalations && escalations.length > 0 && (
        <ol className="history">
          {escalations.map((item) => (
            <li key={item.id}>
              <p>
                <strong>
                  {item.fromPriority} → {item.toPriority}
                </strong>
              </p>
              <p>Agent: {agentChange(item.fromAgent, item.toAgent)}</p>
              <p>
                Due: {formatDateTime(item.fromDueAtUtc)} → {formatDateTime(item.toDueAtUtc)}
              </p>
              <p>Reason: {item.reason}</p>
              <p className="summary__note">
                {item.escalatedBy}, {formatDateTime(item.escalatedAtUtc)}
              </p>
            </li>
          ))}
        </ol>
      )}
    </div>
  );
}

function agentChange(from: AgentSummary | null, to: AgentSummary | null): string {
  const toName = to?.fullName ?? 'Unassigned';

  return from?.id === to?.id
    ? `${toName} (unchanged)`
    : `${from?.fullName ?? 'Unassigned'} → ${toName}`;
}
