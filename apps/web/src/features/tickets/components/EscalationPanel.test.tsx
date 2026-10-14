import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import { ticketDetail } from '../../../test/fixtures';
import { EscalationPanel } from './EscalationPanel';

vi.mock('../../../api/tickets', () => ({
  ticketsApi: { escalate: vi.fn() },
}));

const escalate = vi.mocked(ticketsApi.escalate);

function type(reason: string) {
  fireEvent.change(screen.getByLabelText(/reason for escalation/i), { target: { value: reason } });
}

describe('EscalationPanel', () => {
  beforeEach(() => vi.clearAllMocks());

  it('blocks a too-short reason, shows the field error and does not call the API', () => {
    render(<EscalationPanel ticket={ticketDetail()} onEscalated={vi.fn()} />);

    type('ok');
    fireEvent.click(screen.getByRole('button', { name: /escalate ticket/i }));

    expect(screen.getByText(/at least 5 characters/i)).toBeInTheDocument();
    expect(escalate).not.toHaveBeenCalled();
  });

  it('shows a 409 from the server and reports no change', async () => {
    const message = 'Ticket TCK-0012 is already Critical and cannot be escalated further.';
    escalate.mockRejectedValue(new ApiError(409, message));
    const onEscalated = vi.fn();

    render(
      <EscalationPanel ticket={ticketDetail()} onEscalated={onEscalated} />,
    );

    type('The whole site is down');
    fireEvent.click(screen.getByRole('button', { name: /escalate ticket/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent(message);
    expect(escalate).toHaveBeenCalledWith(12, 'The whole site is down');
    expect(onEscalated).not.toHaveBeenCalled();
  });

  it('disables the control when the ticket is already Critical', () => {
    render(
      <EscalationPanel
        ticket={ticketDetail({ priority: 'Critical' })}
        onEscalated={vi.fn()}
      />,
    );

    expect(screen.getByRole('button', { name: /escalate ticket/i })).toBeDisabled();
    expect(screen.getByText(/already Critical/i)).toBeInTheDocument();
  });
});
