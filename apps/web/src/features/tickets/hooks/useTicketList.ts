import { useCallback, useEffect, useMemo, useState } from 'react';
import { ApiError } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import type { PagedResult, TicketFilters, TicketListItem } from '../../../types/api';

const DEBOUNCE_MS = 300;

export const emptyFilters: TicketFilters = {
  search: '',
  status: undefined,
  priority: undefined,
  slaStatus: undefined,
  categoryId: undefined,
  customerId: undefined,
  assignedAgentId: undefined,
  unassignedOnly: false,
  page: 1,
  pageSize: 20,
  sortBy: 'createdAtUtc',
  sortDirection: 'desc',
};

interface State {
  isLoading: boolean;
  queryKey?: string;
  data?: PagedResult<TicketListItem>;
  error?: string;
}

function useDebouncedValue<T>(value: T, delayMs: number): T {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(value), delayMs);
    return () => window.clearTimeout(handle);
  }, [value, delayMs]);

  return debounced;
}

/** Owns the ticket list's filters and the data they produce. */
export function useTicketList() {
  const [filters, setFilters] = useState<TicketFilters>(emptyFilters);
  const [state, setState] = useState<State>({ isLoading: true });
  const debouncedSearch = useDebouncedValue(filters.search, DEBOUNCE_MS);
  const query = useMemo(
    () => ({
      search: debouncedSearch,
      status: filters.status,
      priority: filters.priority,
      slaStatus: filters.slaStatus,
      categoryId: filters.categoryId,
      customerId: filters.customerId,
      assignedAgentId: filters.assignedAgentId,
      unassignedOnly: filters.unassignedOnly,
      page: filters.page,
      pageSize: filters.pageSize,
      sortBy: filters.sortBy,
      sortDirection: filters.sortDirection,
    }),
    [
      debouncedSearch,
      filters.status,
      filters.priority,
      filters.slaStatus,
      filters.categoryId,
      filters.customerId,
      filters.assignedAgentId,
      filters.unassignedOnly,
      filters.page,
      filters.pageSize,
      filters.sortBy,
      filters.sortDirection,
    ],
  );
  const queryKey = JSON.stringify(query);

  const load = useCallback(
    (isCurrent: () => boolean = () => true) =>
      ticketsApi.getTickets(query).then(
        (data) => {
          if (isCurrent()) setState({ isLoading: false, queryKey, data, error: undefined });
        },
        (error: unknown) => {
          if (!isCurrent()) return;
          const message = error instanceof ApiError ? error.message : 'Unable to load tickets.';
          setState((current) => ({ ...current, isLoading: false, queryKey, error: message }));
        },
      ),
    [query, queryKey],
  );

  useEffect(() => {
    let cancelled = false;
    void load(() => !cancelled);

    return () => {
      cancelled = true;
    };
  }, [load]);

  const updateFilters = (patch: Partial<TicketFilters>) =>
    setFilters((current) => ({ ...current, ...patch, page: patch.page ?? 1 }));

  const resetFilters = () => setFilters(emptyFilters);

  const reload = () => {
    setState((current) => ({ ...current, isLoading: true, error: undefined }));
    return load();
  };

  return {
    filters,
    updateFilters,
    resetFilters,
    reload,
    ...state,
    isLoading: state.isLoading || state.queryKey !== queryKey,
  };
}
