import { FileMetadata } from "../types/file";

const HISTORY_KEY = "file_sharing_history";

export const historyService = {
  getHistory(): FileMetadata[] {
    try {
      const data = localStorage.getItem(HISTORY_KEY);
      if (!data) return [];
      const list = JSON.parse(data) as FileMetadata[];
      return list
        .map(item => {
          // Sanitize: cap downloadCount at maxDownloads for any stale data
          if (item.maxDownloads !== undefined && item.maxDownloads > 0) {
            return { ...item, downloadCount: Math.min(item.downloadCount ?? 0, item.maxDownloads) };
          }
          return item;
        });
    } catch (e) {
      console.error("Error reading upload history", e);
      return [];
    }
  },

  addToHistory(file: FileMetadata): void {
    try {
      const history = this.getHistory();
      // Avoid duplicates
      const filtered = history.filter(item => item.code !== file.code);
      const updated = [file, ...filtered];
      localStorage.setItem(HISTORY_KEY, JSON.stringify(updated));
    } catch (e) {
      console.error("Error adding to history", e);
    }
  },

  removeFromHistory(code: string): void {
    try {
      const history = this.getHistory();
      const updated = history.filter(item => item.code !== code);
      localStorage.setItem(HISTORY_KEY, JSON.stringify(updated));
    } catch (e) {
      console.error("Error removing from history", e);
    }
  },
  
  updateDownloadCount(code: string): void {
    try {
      const history = this.getHistory();
      const updated = history.map(item => {
        if (item.code === code) {
          const current = item.downloadCount ?? 0;
          const max = item.maxDownloads;
          // Cap at maxDownloads so we never exceed the limit
          const next = max !== undefined && max > 0 ? Math.min(current + 1, max) : current + 1;
          return { ...item, downloadCount: next };
        }
        return item;
      });
      localStorage.setItem(HISTORY_KEY, JSON.stringify(updated));
    } catch (e) {
      console.error("Error updating download count in history", e);
    }
  }
};
