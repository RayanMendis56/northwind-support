import { act, renderHook } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ticketsApi } from '../../../api/tickets';
import { page, ticketListItem } from '../../../test/fixtures';
import { useTicketList } from './useTicketList';

vi.mock('../../../api/tickets', () => ({
  ticketsApi: {
    getTickets: vi.fn(),
  },
}));

const getTickets = vi.mocked(ticketsApi.getTickets);

describe('useTicketList', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    getTickets.mockResolvedValue(page([ticketListItem()]));
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.clearAllMocks();
  });

  it('searches with the text the user typed, once the typing settles', async () => {
    const { result } = renderHook(() => useTicketList());

    await act(async () => {
      await vi.advanceTimersByTimeAsync(300);
    });

    expect(getTickets).toHaveBeenCalledTimes(1);

    act(() => {
      result.current.updateFilters({ search: 'invoice' });
    });

    // Still inside the debounce window: no second request yet.
    expect(getTickets).toHaveBeenCalledTimes(1);

    await act(async () => {
      await vi.advanceTimersByTimeAsync(300);
    });

    expect(getTickets).toHaveBeenCalledTimes(2);
    expect(getTickets).toHaveBeenLastCalledWith(
      expect.objectContaining({ search: 'invoice', page: 1 }),
    );

    expect(result.current.isLoading).toBe(false);
    expect(result.current.data?.items).toHaveLength(1);
  });

  it('sends a request immediately when the status changes', async () => {
    const { result } = renderHook(() => useTicketList());

    await act(async () => {
      await vi.advanceTimersByTimeAsync(300);
    });
    expect(getTickets).toHaveBeenCalledTimes(1);

    act(() => {
      result.current.updateFilters({ status: 'InProgress' });
    });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(0);
    });

    expect(getTickets).toHaveBeenCalledTimes(2);
    expect(getTickets).toHaveBeenLastCalledWith(
      expect.objectContaining({ status: 'InProgress' }),
    );
  });

  it('sends a request immediately when the SLA status changes', async () => {
    const { result } = renderHook(() => useTicketList());

    await act(async () => {
      await vi.advanceTimersByTimeAsync(300);
    });

    act(() => {
      result.current.updateFilters({ slaStatus: 'AtRisk' });
    });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(0);
    });

    expect(getTickets).toHaveBeenLastCalledWith(
      expect.objectContaining({ slaStatus: 'AtRisk' }),
    );
  });

  it('keeps the chosen status when the search is typed and cleared', async () => {
    const { result } = renderHook(() => useTicketList());
    await act(async () => {
      await vi.advanceTimersByTimeAsync(300);
    });

    act(() => result.current.updateFilters({ status: 'InProgress' }));
    await act(async () => {
      await vi.advanceTimersByTimeAsync(0);
    });

    act(() => result.current.updateFilters({ search: 'invoice' }));
    await act(async () => {
      await vi.advanceTimersByTimeAsync(300);
    });
    expect(getTickets).toHaveBeenLastCalledWith(
      expect.objectContaining({ status: 'InProgress', search: 'invoice' }),
    );

    act(() => result.current.updateFilters({ search: '' }));
    await act(async () => {
      await vi.advanceTimersByTimeAsync(300);
    });
    expect(getTickets).toHaveBeenLastCalledWith(
      expect.objectContaining({ status: 'InProgress', search: '' }),
    );
  });
});
