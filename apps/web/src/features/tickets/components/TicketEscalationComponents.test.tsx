import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { Escalation, TicketDetail } from '../../../types/api';
import { EscalationHistory } from './EscalationHistory';
import { TriageResult } from './TriageResult';

const ticket: TicketDetail = {
  id: 7,
  reference: 'TCK-0007',
  title: 'Portal is unavailable',
  description: 'The portal is unavailable to our team.',
  status: 'InProgress',
  priority: 'High',
  customer: {
    id: 1,
    name: 'Contoso',
    tier: 'Premium',
    email: 'help@contoso.example',
    phone: null,
  },
  category: { id: 3, name: 'Technical', requiresSpecialist: false },
  assignedAgent: { id: 2, fullName: 'Alex Turner' },
  createdAtUtc: '2026-10-14T09:00:00Z',
  updatedAtUtc: '2026-10-14T09:00:00Z',
  dueAtUtc: '2026-10-14T13:00:00Z',
  resolvedAtUtc: null,
  slaStatus: 'WithinSla',
};

describe('EscalationHistory', () => {
  it('shows the empty state when there are no entries', () => {
    render(<EscalationHistory escalations={[]} isLoading={false} onRetry={vi.fn()} />);

    expect(screen.getByText('No escalations yet')).toBeInTheDocument();
  });

  it('shows the transition reason and actor for a history entry', () => {
    const entry: Escalation = {
      id: 1,
      fromPriority: 'High',
      toPriority: 'Critical',
      fromAgent: null,
      toAgent: { id: 2, fullName: 'Alex Turner' },
      fromDueAtUtc: null,
      toDueAtUtc: '2026-10-14T13:00:00Z',
      reason: 'Customer impact increased',
      escalatedBy: 'alex.lead',
      escalatedAtUtc: '2026-10-14T09:00:00Z',
    };

    render(<EscalationHistory escalations={[entry]} isLoading={false} onRetry={vi.fn()} />);

    expect(screen.getByText('High → Critical')).toBeInTheDocument();
    expect(screen.getByText('Agent: Unassigned → Alex Turner')).toBeInTheDocument();
    expect(screen.getByText('Reason: Customer impact increased')).toBeInTheDocument();
    expect(screen.getByText(/alex\.lead/)).toBeInTheDocument();
  });
});

describe('TriageResult', () => {
  it('shows nothing when the response has no triage decision', () => {
    const { container } = render(<TriageResult ticket={ticket} />);

    expect(container).toBeEmptyDOMElement();
  });

  it('shows the priority, SLA, and assignment reasons', () => {
    render(
      <TriageResult
        ticket={{
          ...ticket,
          triage: {
            appliedPriority: 'Critical',
            priorityReason: 'Category forces Critical',
            slaWindowHours: 2,
            slaReason: 'Premium response window',
            assignmentReason: 'Fewest open tickets among specialists',
          },
        }}
      />,
    );

    expect(screen.getByText('Category forces Critical')).toBeInTheDocument();
    expect(screen.getByText('2h window. Premium response window')).toBeInTheDocument();
    expect(screen.getByText('Fewest open tickets among specialists')).toBeInTheDocument();
  });
});
