import { describe, expect, it } from 'vitest';
import { escalationBlockedReason, validateReason } from './escalationRules';

describe('validateReason', () => {
  it('measures the minimum length after trimming', () => {
    expect(validateReason('  ok  ')).toBe('Give a reason of at least 5 characters.');
    expect(validateReason('abcde')).toBeUndefined();
    expect(validateReason('  outage  ')).toBeUndefined();
  });

  it('rejects a reason longer than the server limit after trimming', () => {
    expect(validateReason(` ${'x'.repeat(501)} `)).toBe(
      'Keep the reason to 500 characters or fewer.',
    );
  });
});

describe('escalationBlockedReason', () => {
  it.each(['Resolved', 'Closed'] as const)('blocks a %s ticket', (status) => {
    expect(escalationBlockedReason({ priority: 'Low', status })).toBe(
      `This ticket is ${status} and cannot be escalated.`,
    );
  });

  it('blocks a Critical ticket', () => {
    expect(escalationBlockedReason({ priority: 'Critical', status: 'Open' })).toBe(
      'This ticket is already Critical and cannot be escalated further.',
    );
  });

  it('allows an open ticket below Critical', () => {
    expect(escalationBlockedReason({ priority: 'High', status: 'InProgress' })).toBeUndefined();
  });
});
