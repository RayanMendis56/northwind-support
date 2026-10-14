import { useState } from 'react';
import { toErrorMessage } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import { Field } from '../../../components/Field';
import { ESCALATION_REASON_MAX, ESCALATION_REASON_MIN } from '../../../types/api';
import type { EscalateResponse, TicketDetail } from '../../../types/api';
import { escalationBlockedReason, validateReason } from '../escalationRules';

interface EscalationPanelProps {
  ticket: TicketDetail;
  onEscalated: (result: EscalateResponse) => void;
}

/** Escalates a ticket with a reason. Validates the server's rules before calling the API. */
export function EscalationPanel({ ticket, onEscalated }: EscalationPanelProps) {
  const [reason, setReason] = useState('');
  const [fieldError, setFieldError] = useState<string>();
  const [submitError, setSubmitError] = useState<string>();
  const [isSaving, setIsSaving] = useState(false);

  const blockedReason = escalationBlockedReason(ticket);

  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSubmitError(undefined);

    const problem = validateReason(reason);
    setFieldError(problem);

    if (problem) {
      return;
    }

    setIsSaving(true);

    try {
      const result = await ticketsApi.escalate(ticket.id, reason.trim());
      setReason('');
      onEscalated(result);
    } catch (caught) {
      setSubmitError(toErrorMessage(caught, 'Could not escalate the ticket.'));
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <form className="card form" onSubmit={submit} noValidate>
      <h2>Escalate</h2>

      {blockedReason && <p className="summary__note">{blockedReason}</p>}

      <Field
        id="escalation-reason"
        label="Reason for escalation"
        error={fieldError}
        hint={`${ESCALATION_REASON_MIN} to ${ESCALATION_REASON_MAX} characters.`}
      >
        {(fieldProps) => (
          <textarea
            {...fieldProps}
            rows={4}
            value={reason}
            disabled={blockedReason !== undefined || isSaving}
            onChange={(event) => setReason(event.target.value)}
          />
        )}
      </Field>

      {submitError && (
        <p className="field__error" role="alert">
          {submitError}
        </p>
      )}

      <div className="button-row">
        <button
          type="submit"
          className="button button--primary"
          disabled={blockedReason !== undefined || isSaving}
        >
          {isSaving ? 'Escalating...' : 'Escalate ticket'}
        </button>
      </div>
    </form>
  );
}
