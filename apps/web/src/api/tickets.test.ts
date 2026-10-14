import { beforeEach, describe, expect, it, vi } from 'vitest';
import { request } from './client';
import { ticketsApi } from './tickets';

vi.mock('./client', () => ({
  request: vi.fn(),
}));

const mockedRequest = vi.mocked(request);

describe('ticketsApi', () => {
  beforeEach(() => {
    mockedRequest.mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
      totalPages: 0,
    });
  });

  it('sends the SLA status as a list query parameter', async () => {
    await ticketsApi.getTickets({
      search: '',
      slaStatus: 'AtRisk',
      page: 1,
      pageSize: 20,
      sortBy: 'createdAtUtc',
      sortDirection: 'desc',
    });

    expect(mockedRequest).toHaveBeenCalledWith(
      expect.stringContaining('slaStatus=AtRisk'),
    );
  });

  it('requests escalation history for a ticket', async () => {
    await ticketsApi.getEscalations(42);

    expect(mockedRequest).toHaveBeenCalledWith('/tickets/42/escalations');
  });

  it('sends the reason and supplied actor when escalating a ticket', async () => {
    await ticketsApi.escalate(42, 'Customer impact increased', 'team.lead');

    expect(mockedRequest).toHaveBeenCalledWith('/tickets/42/escalate', {
      method: 'POST',
      body: JSON.stringify({
        reason: 'Customer impact increased',
        escalatedBy: 'team.lead',
      }),
    });
  });
});
