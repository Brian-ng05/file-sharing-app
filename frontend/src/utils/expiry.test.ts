import { describe, it, expect, vi, beforeEach } from 'vitest';
import { formatExpiryDisplay, type ExpiryDisplay } from './expiry';

describe('formatExpiryDisplay', () => {
  // ──────────────────────────────────────────────
  // NO EXPIRY
  // ──────────────────────────────────────────────

  it('returns active when expiresAt is undefined', () => {
    const result = formatExpiryDisplay(undefined, Date.now());
    expect(result).toEqual({
      relativeText: 'Active',
      localTimeText: null,
      expired: false,
      active: true,
    });
  });

  it('returns active when expiresAt is empty string', () => {
    const result = formatExpiryDisplay('', Date.now());
    expect(result.active).toBe(true);
    expect(result.expired).toBe(false);
  });

  // ──────────────────────────────────────────────
  // EXPIRED
  // ──────────────────────────────────────────────

  it('returns expired when expiry is in the past', () => {
    const pastDate = new Date(Date.now() - 1000 * 60 * 60); // 1 hour ago
    const result = formatExpiryDisplay(pastDate.toISOString(), Date.now());
    expect(result.expired).toBe(true);
    expect(result.active).toBe(false);
    expect(result.relativeText).toBe('Expired');
  });

  it('returns expired when expiry is exactly now', () => {
    const now = Date.now();
    const result = formatExpiryDisplay(new Date(now).toISOString(), now);
    expect(result.expired).toBe(true);
    expect(result.relativeText).toBe('Expired');
  });

  // ──────────────────────────────────────────────
  // ACTIVE WITH DAYS LEFT
  // ──────────────────────────────────────────────

  it('returns days left when >1 day remaining', () => {
    const futureDate = new Date(Date.now() + 1000 * 60 * 60 * 24 * 3); // 3 days
    const result = formatExpiryDisplay(futureDate.toISOString(), Date.now());
    expect(result.expired).toBe(false);
    expect(result.active).toBe(false);
    expect(result.relativeText).toBe('3 days left');
  });

  it('returns singular day when exactly 1 day remaining', () => {
    const futureDate = new Date(Date.now() + 1000 * 60 * 60 * 24); // 1 day
    const result = formatExpiryDisplay(futureDate.toISOString(), Date.now());
    expect(result.relativeText).toBe('1 day left');
  });

  // ──────────────────────────────────────────────
  // ACTIVE WITH HOURS LEFT
  // ──────────────────────────────────────────────

  it('returns hours left when >1 hour remaining', () => {
    const futureDate = new Date(Date.now() + 1000 * 60 * 60 * 5); // 5 hours
    const result = formatExpiryDisplay(futureDate.toISOString(), Date.now());
    expect(result.relativeText).toBe('5 hours left');
  });

  it('returns singular hour when exactly 1 hour remaining', () => {
    const futureDate = new Date(Date.now() + 1000 * 60 * 60); // 1 hour
    const result = formatExpiryDisplay(futureDate.toISOString(), Date.now());
    expect(result.relativeText).toBe('1 hour left');
  });

  // ──────────────────────────────────────────────
  // ACTIVE WITH MINUTES LEFT
  // ──────────────────────────────────────────────

  it('returns minutes left when >1 minute remaining', () => {
    const futureDate = new Date(Date.now() + 1000 * 60 * 30); // 30 minutes
    const result = formatExpiryDisplay(futureDate.toISOString(), Date.now());
    expect(result.relativeText).toBe('30 minutes left');
  });

  it('returns singular minute when exactly 1 minute remaining', () => {
    const futureDate = new Date(Date.now() + 1000 * 60); // 1 minute
    const result = formatExpiryDisplay(futureDate.toISOString(), Date.now());
    expect(result.relativeText).toBe('1 minute left');
  });

  it('returns less than 1 minute for <60 seconds', () => {
    const futureDate = new Date(Date.now() + 1000 * 30); // 30 seconds
    const result = formatExpiryDisplay(futureDate.toISOString(), Date.now());
    expect(result.relativeText).toBe('Less than 1 minute left');
  });

  it('returns less than 1 minute for <1 second', () => {
    const futureDate = new Date(Date.now() + 500); // 500ms
    const result = formatExpiryDisplay(futureDate.toISOString(), Date.now());
    expect(result.relativeText).toBe('Less than 1 minute left');
  });

  // ──────────────────────────────────────────────
  // LOCAL TIME TEXT
  // ──────────────────────────────────────────────

  it('includes localTimeText with ICT format when expiry is set', () => {
    const futureDate = new Date(Date.now() + 1000 * 60 * 60 * 24);
    const result = formatExpiryDisplay(futureDate.toISOString(), Date.now());
    expect(result.localTimeText).toBeTruthy();
    expect(result.localTimeText).toContain('ICT');
  });

  // ──────────────────────────────────────────────
  // EDGE CASES
  // ──────────────────────────────────────────────

  it('handles very far future date', () => {
    const farFuture = new Date(Date.now() + 1000 * 60 * 60 * 24 * 365); // 1 year
    const result = formatExpiryDisplay(farFuture.toISOString(), Date.now());
    expect(result.expired).toBe(false);
    expect(result.relativeText).toContain('days left');
  });

  it('handles very far past date', () => {
    const farPast = new Date(Date.now() - 1000 * 60 * 60 * 24 * 365); // 1 year ago
    const result = formatExpiryDisplay(farPast.toISOString(), Date.now());
    expect(result.expired).toBe(true);
    expect(result.relativeText).toBe('Expired');
  });
});
