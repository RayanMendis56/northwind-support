import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { SlaStatus } from '../../../types/api';
import { SlaIndicator } from './SlaIndicator';
import { slaPresentation, slaStatuses } from './slaStatus';

describe('SlaIndicator', () => {
  it.each(slaStatuses)('renders %s with its own text label', (status: SlaStatus) => {
    render(<SlaIndicator slaStatus={status} />);

    expect(screen.getByText(slaPresentation[status].text)).toBeInTheDocument();
  });

  it('never uses the same label or symbol for two states', () => {
    const labels = slaStatuses.map((status) => slaPresentation[status].text);
    const symbols = slaStatuses.map((status) => slaPresentation[status].symbol);

    expect(new Set(labels).size).toBe(slaStatuses.length);
    expect(new Set(symbols).size).toBe(slaStatuses.length);
  });
});
