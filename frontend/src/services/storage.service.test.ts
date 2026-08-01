import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { storageService } from './storage.service';

describe('storageService', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  // ──────────────────────────────────────────────
  // SET
  // ──────────────────────────────────────────────

  it('sets a string value', () => {
    storageService.set('key1', 'hello');
    expect(localStorage.getItem('key1')).toBe('"hello"');
  });

  it('sets an object value', () => {
    storageService.set('user', { name: 'John', age: 30 });
    const stored = localStorage.getItem('user');
    expect(JSON.parse(stored!)).toEqual({ name: 'John', age: 30 });
  });

  it('sets a number value', () => {
    storageService.set('count', 42);
    expect(localStorage.getItem('count')).toBe('42');
  });

  it('sets an array value', () => {
    storageService.set('list', [1, 2, 3]);
    const stored = localStorage.getItem('list');
    expect(JSON.parse(stored!)).toEqual([1, 2, 3]);
  });

  // ──────────────────────────────────────────────
  // GET
  // ──────────────────────────────────────────────

  it('gets a stored value', () => {
    localStorage.setItem('key1', '"hello"');
    const result = storageService.get<string>('key1');
    expect(result).toBe('hello');
  });

  it('returns null for non-existent key', () => {
    const result = storageService.get<string>('nonexistent');
    expect(result).toBeNull();
  });

  it('gets a stored object', () => {
    localStorage.setItem('obj', JSON.stringify({ a: 1, b: 'two' }));
    const result = storageService.get<{ a: number; b: string }>('obj');
    expect(result).toEqual({ a: 1, b: 'two' });
  });

  it('returns null for invalid JSON', () => {
    localStorage.setItem('bad', 'not-json{{{');
    const result = storageService.get<string>('bad');
    expect(result).toBeNull();
  });

  // ──────────────────────────────────────────────
  // REMOVE
  // ──────────────────────────────────────────────

  it('removes a key', () => {
    storageService.set('key1', 'value');
    storageService.remove('key1');
    expect(localStorage.getItem('key1')).toBeNull();
  });

  it('does not throw when removing non-existent key', () => {
    expect(() => storageService.remove('nonexistent')).not.toThrow();
  });

  // ──────────────────────────────────────────────
  // CLEAR
  // ──────────────────────────────────────────────

  it('clears all storage', () => {
    storageService.set('a', 1);
    storageService.set('b', 2);
    storageService.set('c', 3);

    storageService.clear();

    expect(localStorage.getItem('a')).toBeNull();
    expect(localStorage.getItem('b')).toBeNull();
    expect(localStorage.getItem('c')).toBeNull();
  });

  // ──────────────────────────────────────────────
  // ROUND-TRIP
  // ──────────────────────────────────────────────

  it('round-trips complex objects', () => {
    const complex = {
      id: 1,
      name: 'Test',
      tags: ['a', 'b'],
      nested: { key: 'value' },
      date: new Date().toISOString(),
    };

    storageService.set('complex', complex);
    const result = storageService.get<typeof complex>('complex');

    expect(result).toEqual(complex);
  });
});
