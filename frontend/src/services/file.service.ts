import { FileMetadata, UploadOptions } from "../types/file";
import { API_BASE_URL, isMockMode } from "./api";
import { historyService } from "./history.service";

// Helper for simulating api delays in mock mode
const delay = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

// Max file size allowed (10 MB)
const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10,485,760 bytes

// Caches for file Blobs to prevent duplicate downloads (mock mode only)
const blobCache = new Map<string, Blob>();

// Helper to convert base64 data URLs to Blobs in mock mode
function dataURLtoBlob(dataurl: string): Blob {
  const arr = dataurl.split(',');
  const mime = arr[0].match(/:(.*?);/)?.[1] || 'application/octet-stream';
  const bstr = atob(arr[1]);
  let n = bstr.length;
  const u8arr = new Uint8Array(n);
  while (n--) {
    u8arr[n] = bstr.charCodeAt(n);
  }
  return new Blob([u8arr], { type: mime });
}

/**
 * Maps HTTP status codes to user-friendly error messages.
 */
function mapHttpError(status: number, defaultMsg?: string): string {
  switch (status) {
    case 400: return "Invalid request. Please check your input.";
    case 401: return "Password is required or invalid password.";
    case 404: return "File not found or has expired.";
    case 410: return "This file has expired or is unavailable.";
    case 413: return "File size exceeds the maximum limit of 10 MB.";
    case 502:
    case 503: return "Storage service is temporarily unavailable. Please try again later.";
    default: return defaultMsg || `Request failed with status ${status}.`;
  }
}

export const fileService = {
  /**
   * Returns whether we are currently in mock mode.
   */
  isMockMode,

  /**
   * Retrieves a cached Blob if available (mock mode only).
   */
  getCachedBlob(code: string): Blob | undefined {
    return blobCache.get(code);
  },

  /**
   * Uploads a file with options (max downloads, expiry time, password) and reports progress.
   */
  async uploadFile(
    file: File,
    options?: UploadOptions,
    onProgress?: (progress: number) => void
  ): Promise<FileMetadata> {
    // 1. Frontend validation
    if (file.size > MAX_FILE_SIZE) {
      throw new Error(`File size exceeds the maximum limit of 10 MB. Your file: ${(file.size / (1024 * 1024)).toFixed(2)} MB`);
    }

    const password = options?.password?.trim() || undefined;
    const requiresPassword = !!password;

    if (isMockMode()) {
      // Simulate progressive incremental upload reporting in Mock Mode
      const steps = [10, 30, 50, 70, 85, 95, 100];
      for (const step of steps) {
        if (onProgress) {
          onProgress(step);
        }
        await delay(120); // total ~0.8s upload latency
      }

      // Generate a short 6-character alphanumeric code
      const chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
      let code = "";
      for (let i = 0; i < 6; i++) {
        code += chars.charAt(Math.floor(Math.random() * chars.length));
      }

      // Read file content as base64 if it's small enough (< 2MB) to save in localStorage mock
      let contentDataUrl = "";
      if (file.size <= 2 * 1024 * 1024) {
        contentDataUrl = await new Promise<string>((resolve) => {
          const reader = new FileReader();
          reader.onloadend = () => resolve(reader.result as string);
          reader.readAsDataURL(file);
        });
      } else {
        // For larger files, create a mock placeholder to avoid local storage quota limits
        contentDataUrl = `data:text/plain;base64,${btoa(`Mock content placeholder for large file: ${file.name}`)}`;
      }

      const expiresAt = options?.expiryHours
        ? new Date(Date.now() + options.expiryHours * 60 * 60 * 1000).toISOString()
        : undefined;

      const metadata: FileMetadata = {
        code,
        originalFileName: file.name,
        mimeType: file.type || "application/octet-stream",
        sizeBytes: file.size,
        requiresPassword,
        maxDownloads: options?.maxDownloads || undefined,
        downloadCount: 0,
        expiresAt,
        createdAt: new Date().toISOString(),
      };

      // Store in LocalStorage mock registry
      localStorage.setItem(`mock_file_meta_${code}`, JSON.stringify(metadata));
      localStorage.setItem(`mock_file_content_${code}`, contentDataUrl);
      if (password) {
        localStorage.setItem(`mock_file_password_${code}`, password);
      }

      // Add to user's local history
      historyService.addToHistory(metadata);

      return metadata;
    } else {
      // Real API Call using XMLHttpRequest to track upload progress
      return new Promise<FileMetadata>((resolve, reject) => {
        const xhr = new XMLHttpRequest();
        const formData = new FormData();
        formData.append("file", file, file.name);

        if (options?.maxDownloads !== undefined && options.maxDownloads > 0) {
          formData.append("maxDownloads", options.maxDownloads.toString());
        }

        if (options?.expiryHours !== undefined && options.expiryHours > 0) {
          const expiresAtDate = new Date(Date.now() + options.expiryHours * 60 * 60 * 1000);
          formData.append("expiresAt", expiresAtDate.toISOString());
        }

        if (password) {
          formData.append("password", password);
        }

        // Attach progress listener
        if (xhr.upload && onProgress) {
          xhr.upload.addEventListener("progress", (event) => {
            if (event.lengthComputable) {
              const percentComplete = Math.round((event.loaded / event.total) * 100);
              onProgress(percentComplete);
            }
          });
        }

        // Handle load completion
        xhr.onload = () => {
          if (xhr.status >= 200 && xhr.status < 300) {
            try {
              const resData = JSON.parse(xhr.responseText) as { code: string; downloadUrl: string };

              const expiresAt = options?.expiryHours
                ? new Date(Date.now() + options.expiryHours * 60 * 60 * 1000).toISOString()
                : undefined;

              const metadata: FileMetadata = {
                code: resData.code,
                originalFileName: file.name,
                mimeType: file.type || "application/octet-stream",
                sizeBytes: file.size,
                requiresPassword,
                maxDownloads: options?.maxDownloads || undefined,
                downloadCount: 0,
                expiresAt,
                createdAt: new Date().toISOString(),
              };

              historyService.addToHistory(metadata);
              resolve(metadata);
            } catch (e) {
              reject(new Error("Failed to parse response metadata from server."));
            }
          } else {
            reject(new Error(mapHttpError(xhr.status, xhr.responseText || `Upload failed with status code ${xhr.status}`)));
          }
        };

        xhr.onerror = () => {
          reject(new Error("A network error occurred during file upload."));
        };

        xhr.open("POST", `${API_BASE_URL}/files`);
        xhr.send(formData);
      });
    }
  },

  /**
   * Fetches metadata for a file by code.
   * In real API mode, calls GET /files/{code}/info (does NOT increment download count).
   * In mock mode, reads from localStorage.
   */
  async getFileMetadata(code: string): Promise<FileMetadata> {
    if (isMockMode()) {
      await delay(400);

      const metaStr = localStorage.getItem(`mock_file_meta_${code}`);
      if (!metaStr) {
        throw new Error("File not found or has been deleted.");
      }

      const metadata = JSON.parse(metaStr) as FileMetadata;
      const now = new Date();

      // Check Expiry
      if (metadata.expiresAt && new Date(metadata.expiresAt) < now) {
        this.deleteMockData(code);
        throw new Error("This file has expired and is no longer available.");
      }

      // Check Download Limit
      if (metadata.maxDownloads !== undefined && (metadata.downloadCount ?? 0) >= metadata.maxDownloads) {
        this.deleteMockData(code);
        throw new Error("This file has reached its download limit.");
      }

      // Cache the mock blob
      const content = localStorage.getItem(`mock_file_content_${code}`);
      if (content) {
        try {
          const blob = dataURLtoBlob(content);
          blobCache.set(code, blob);
        } catch (e) {
          console.error("Failed to parse base64 mock file content to Blob", e);
        }
      }

      return metadata;
    } else {
      // Real API: call GET /files/{code}/info
      const response = await fetch(`${API_BASE_URL}/files/${encodeURIComponent(code)}/info`);

      if (!response.ok) {
        if (response.status === 404) {
          throw new Error("File not found or has expired.");
        }
        if (response.status === 410) {
          throw new Error("This file has expired or is unavailable.");
        }
        throw new Error(mapHttpError(response.status, `Failed to fetch file info: ${response.statusText}`));
      }

      const data = await response.json() as {
        code: string;
        originalFilename: string;
        mimeType: string;
        sizeBytes: number;
        requiresPassword: boolean;
        expiresAt?: string;
        createdAt: string;
      };

      // Map backend response (camelCase from System.Text.Json) to our FileMetadata type
      const metadata: FileMetadata = {
        code: data.code,
        originalFileName: data.originalFilename,
        mimeType: data.mimeType,
        sizeBytes: data.sizeBytes,
        requiresPassword: data.requiresPassword,
        expiresAt: data.expiresAt,
        createdAt: data.createdAt,
      };

      // Merge local uploader history metadata if present (e.g. maxDownloads, downloadCount)
      const localHistory = historyService.getHistory();
      const matchedLocal = localHistory.find((item) => item.code === code);
      if (matchedLocal) {
        metadata.maxDownloads = matchedLocal.maxDownloads;
        metadata.downloadCount = matchedLocal.downloadCount;
        if (!metadata.expiresAt && matchedLocal.expiresAt) {
          metadata.expiresAt = matchedLocal.expiresAt;
        }
      }

      return metadata;
    }
  },

  /**
   * Verifies a file's password on the server.
   * In mock mode, checks against the locally stored password.
   */
  async verifyPassword(code: string, password: string): Promise<boolean> {
    if (isMockMode()) {
      await delay(300);
      const storedPassword = localStorage.getItem(`mock_file_password_${code}`);
      if (!storedPassword) {
        throw new Error("This file does not have a password.");
      }
      if (storedPassword !== password) {
        throw new Error("Invalid password.");
      }
      return true;
    } else {
      const response = await fetch(
        `${API_BASE_URL}/files/${encodeURIComponent(code)}/verify-password`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ password }),
        }
      );

      if (response.status === 404) {
        throw new Error("File not found.");
      }
      if (response.status === 401) {
        throw new Error("Invalid password.");
      }
      if (!response.ok) {
        throw new Error(mapHttpError(response.status, "Failed to verify password."));
      }

      const data = await response.json() as { valid: boolean };
      if (!data.valid) {
        throw new Error("Invalid password.");
      }
      return true;
    }
  },

  /**
   * Deletes a file by code.
   */
  async deleteFile(code: string): Promise<void> {
    if (isMockMode()) {
      await delay(300);
      this.deleteMockData(code);
    } else {
      const response = await fetch(`${API_BASE_URL}/files/${encodeURIComponent(code)}`, {
        method: "DELETE",
      });
      if (!response.ok) {
        throw new Error(mapHttpError(response.status, `Failed to delete file: ${response.statusText}`));
      }
      historyService.removeFromHistory(code);
      blobCache.delete(code);
    }
  },

  /**
   * Generates the API download URL.
   */
  getDownloadUrl(code: string): string {
    if (isMockMode()) {
      return "#mock-download";
    }
    return `${API_BASE_URL}/files/${encodeURIComponent(code)}`;
  },

  /**
   * Downloads a file.
   * In real API mode, navigates to GET /files/{code}?password=xxx (triggers signed-url redirect).
   * In mock mode, downloads from the cached blob.
   */
  async downloadFile(code: string, fileName: string, password?: string): Promise<void> {
    if (isMockMode()) {
      const cachedBlob = blobCache.get(code);
      if (!cachedBlob) {
        throw new Error("File not found in memory cache.");
      }

      const metaStr = localStorage.getItem(`mock_file_meta_${code}`);
      if (!metaStr) {
        throw new Error("File metadata not found.");
      }

      const metadata = JSON.parse(metaStr) as FileMetadata;
      metadata.downloadCount = (metadata.downloadCount ?? 0) + 1;
      localStorage.setItem(`mock_file_meta_${code}`, JSON.stringify(metadata));
      historyService.updateDownloadCount(code);

      // Check if limit exceeded, delete if so
      if (metadata.maxDownloads !== undefined && (metadata.downloadCount ?? 0) >= metadata.maxDownloads) {
        await delay(500);
        this.deleteMockData(code);
      }

      // Download client-side from cached Blob
      const url = URL.createObjectURL(cachedBlob);
      const link = document.createElement("a");
      link.href = url;
      link.download = fileName;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    } else {
      // Real API: navigate to the download URL (triggers signed-url redirect + download count increment)
      let url = this.getDownloadUrl(code);
      if (password) {
        url += `?password=${encodeURIComponent(password)}`;
      }

      const link = document.createElement("a");
      link.href = url;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);

      // Update local download count in history (best-effort)
      setTimeout(() => {
        historyService.updateDownloadCount(code);
      }, 1000);
    }
  },

  /**
   * Helper to clean up mock storage keys and caches.
   */
  deleteMockData(code: string) {
    localStorage.removeItem(`mock_file_meta_${code}`);
    localStorage.removeItem(`mock_file_content_${code}`);
    localStorage.removeItem(`mock_file_password_${code}`);
    historyService.removeFromHistory(code);
    blobCache.delete(code);
  },
};
