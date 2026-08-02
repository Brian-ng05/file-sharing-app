import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { historyService } from './history.service';
import type { FileMetadata } from '../types/file';

describe('historyService', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  // ──────────────────────────────────────────────
  // GET HISTORY
  // ──────────────────────────────────────────────

  it('returns empty array when no history exists', () => {
    const history = historyService.getHistory();
    expect(history).toEqual([]);
  });

  it('returns empty array when localStorage has invalid JSON', () => {
    localStorage.setItem('file_sharing_history', 'not-json');
    const history = historyService.getHistory();
    expect(history).toEqual([]);
  });

  // ──────────────────────────────────────────────
  // ADD TO HISTORY
  // ──────────────────────────────────────────────

  it('adds a file to empty history', () => {
    const file: FileMetadata = {
      code: 'abc12345',
      originalFilename: 'test.pdf',
      mimeType: 'application/pdf',
      sizeBytes: 1024,
      requiresPassword: false,
      createdAt: new Date().toISOString(),
    };

    historyService.addToHistory(file);
    const history = historyService.getHistory();

    expect(history).toHaveLength(1);
    expect(history[0].code).toBe('abc12345');
    expect(history[0].originalFilename).toBe('test.pdf');
  });

  it('adds multiple files', () => {
    const file1: FileMetadata = { code: 'code1', originalFilename: 'f1.pdf', mimeType: 'application/pdf', sizeBytes: 100, requiresPassword: false, createdAt: new Date().toISOString() };
    const file2: FileMetadata = { code: 'code2', originalFilename: 'f2.pdf', mimeType: 'application/pdf', sizeBytes: 200, requiresPassword: false, createdAt: new Date().toISOString() };

    historyService.addToHistory(file1);
    historyService.addToHistory(file2);

    const history = historyService.getHistory();
    expect(history).toHaveLength(2);
    // Most recent first
    expect(history[0].code).toBe('code2');
    expect(history[1].code).toBe('code1');
  });

  it('avoids duplicates by code when adding', () => {
    const file: FileMetadata = { code: 'dupcode', originalFilename: 'first.pdf', mimeType: 'application/pdf', sizeBytes: 100, requiresPassword: false, createdAt: new Date().toISOString() };
    const updatedFile: FileMetadata = { code: 'dupcode', originalFilename: 'second.pdf', mimeType: 'application/pdf', sizeBytes: 200, requiresPassword: false, createdAt: new Date().toISOString() };

    historyService.addToHistory(file);
    historyService.addToHistory(updatedFile);

    const history = historyService.getHistory();
    expect(history).toHaveLength(1);
    expect(history[0].originalFilename).toBe('second.pdf');
  });

  // ──────────────────────────────────────────────
  // REMOVE FROM HISTORY
  // ──────────────────────────────────────────────

  it('removes a file by code', () => {
    const file1: FileMetadata = { code: 'keep', originalFilename: 'keep.pdf', mimeType: 'application/pdf', sizeBytes: 100, requiresPassword: false, createdAt: new Date().toISOString() };
    const file2: FileMetadata = { code: 'remove', originalFilename: 'remove.pdf', mimeType: 'application/pdf', sizeBytes: 200, requiresPassword: false, createdAt: new Date().toISOString() };

    historyService.addToHistory(file1);
    historyService.addToHistory(file2);
    historyService.removeFromHistory('remove');

    const history = historyService.getHistory();
    expect(history).toHaveLength(1);
    expect(history[0].code).toBe('keep');
  });

  it('does nothing when removing non-existent code', () => {
    const file: FileMetadata = { code: 'exists', originalFilename: 'f.pdf', mimeType: 'application/pdf', sizeBytes: 100, requiresPassword: false, createdAt: new Date().toISOString() };
    historyService.addToHistory(file);
    historyService.removeFromHistory('nonexistent');

    const history = historyService.getHistory();
    expect(history).toHaveLength(1);
    expect(history[0].code).toBe('exists');
  });

  it('removes from empty history without error', () => {
    expect(() => historyService.removeFromHistory('any')).not.toThrow();
  });

  // ──────────────────────────────────────────────
  // UPDATE DOWNLOAD COUNT
  // ──────────────────────────────────────────────

  it('increments download count', () => {
    const file: FileMetadata = { code: 'dlcode', originalFilename: 'dl.pdf', mimeType: 'application/pdf', sizeBytes: 100, requiresPassword: false, downloadCount: 0, maxDownloads: 10, createdAt: new Date().toISOString() };
    historyService.addToHistory(file);
    historyService.updateDownloadCount('dlcode');

    const history = historyService.getHistory();
    expect(history[0].downloadCount).toBe(1);
  });

  it('caps download count at maxDownloads', () => {
    const file: FileMetadata = { code: 'capped', originalFilename: 'cap.pdf', mimeType: 'application/pdf', sizeBytes: 100, requiresPassword: false, downloadCount: 5, maxDownloads: 5, createdAt: new Date().toISOString() };
    historyService.addToHistory(file);
    historyService.updateDownloadCount('capped');

    const history = historyService.getHistory();
    expect(history[0].downloadCount).toBe(5); // capped, not 6
  });

  it('updates with no maxDownloads unlimited', () => {
    const file: FileMetadata = { code: 'unlimited', originalFilename: 'ul.pdf', mimeType: 'application/pdf', sizeBytes: 100, requiresPassword: false, downloadCount: 0, createdAt: new Date().toISOString() };
    historyService.addToHistory(file);
    historyService.updateDownloadCount('unlimited');

    const history = historyService.getHistory();
    expect(history[0].downloadCount).toBe(1);
  });

  it('does nothing when updating non-existent code', () => {
    historyService.updateDownloadCount('nonexistent');
    const history = historyService.getHistory();
    expect(history).toEqual([]);
  });

  // ──────────────────────────────────────────────
  // SANITIZATION
  // ──────────────────────────────────────────────

  it('sanitizes downloadCount to not exceed maxDownloads on read', () => {
    const corrupted = JSON.stringify([{
      code: 'bad', originalFilename: 'bad.pdf', mimeType: 'application/pdf',
      sizeBytes: 100, requiresPassword: false, downloadCount: 10, maxDownloads: 3,
      createdAt: new Date().toISOString()
    }]);
    localStorage.setItem('file_sharing_history', corrupted);

    const history = historyService.getHistory();
    expect(history[0].downloadCount).toBe(3);
  });

  it('sanitizes when maxDownloads is 0 (no cap)', () => {
    const data = JSON.stringify([{
      code: 'zero', originalFilename: 'zero.pdf', mimeType: 'application/pdf',
      sizeBytes: 100, requiresPassword: false, downloadCount: 10, maxDownloads: 0,
      createdAt: new Date().toISOString()
    }]);
    localStorage.setItem('file_sharing_history', data);

    const history = historyService.getHistory();
    // maxDownloads 0 or negative means no sanitization applied
    expect(history[0].downloadCount).toBe(10);
  });
});
