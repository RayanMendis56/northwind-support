import type { BadgeTone } from '../../../components/Badge';
import type { SlaStatus } from '../../../types/api';

export interface SlaPresentation {
  text: string;
  symbol: string;
  tone: BadgeTone;
}

/** Record, not Partial: adding a status to the API type fails the build until it is shown here. */
export const slaPresentation: Record<SlaStatus, SlaPresentation> = {
  WithinSla: { text: 'Within SLA', symbol: '◷', tone: 'info' },
  AtRisk: { text: 'At risk', symbol: '⚠', tone: 'warning' },
  Breached: { text: 'Breached', symbol: '✕', tone: 'danger' },
  Met: { text: 'Met', symbol: '✓', tone: 'success' },
  NotApplicable: { text: 'No SLA', symbol: '–', tone: 'neutral' },
};

export const slaStatuses = Object.keys(slaPresentation) as SlaStatus[];
