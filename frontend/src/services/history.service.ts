import { FileMetadata } from "../types/file";

const HISTORY_KEY = "file_sharing_history";

export const historyService = {
  getHistory(): FileMetadata[] {
    try {
      const data = localStorage.getItem(HISTORY_KEY);
      if (!data) return [];
      const list = JSON.parse(data) as FileMetadata[];
      // Filter out files that we know are already expired based on client time
      const now = new Date();
      return list.filter(item => {
        if (item.expiresAt && new Date(item.expiresAt) < now) {
          return false;
        }
        return true;
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
          return { ...item, downloadCount: item.downloadCount + 1 };
        }
        return item;
      });
      localStorage.setItem(HISTORY_KEY, JSON.stringify(updated));
    } catch (e) {
      console.error("Error updating download count in history", e);
    }
  }
};
