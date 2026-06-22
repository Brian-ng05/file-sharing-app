import { FileMetadata, UploadOptions } from "../types/file";
import { API_BASE_URL, isMockMode } from "./api";
import { historyService } from "./history.service";

// Helper for simulating api delays in mock mode
const delay = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

// Max file size allowed (10 MB)
const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10,485,760 bytes

export const fileService = {
  /**
   * Uploads a file with options (max downloads, expiry time) and reports progress
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
        maxDownloads: options?.maxDownloads || undefined,
        downloadCount: 0,
        expiresAt,
        createdAt: new Date().toISOString(),
      };

      // Store in LocalStorage mock registry
      localStorage.setItem(`mock_file_meta_${code}`, JSON.stringify(metadata));
      localStorage.setItem(`mock_file_content_${code}`, contentDataUrl);

      // Add to user's local history
      historyService.addToHistory(metadata);

      return metadata;
    } else {
      // Real API Call using XMLHttpRequest to track upload progress
      return new Promise<FileMetadata>((resolve, reject) => {
        const xhr = new XMLHttpRequest();
        const formData = new FormData();
        formData.append("file", file);
        
        if (options?.maxDownloads !== undefined && options.maxDownloads > 0) {
          formData.append("maxDownloads", options.maxDownloads.toString());
        }
        
        if (options?.expiryHours !== undefined && options.expiryHours > 0) {
          // Calculate expiresAt datetime string for the backend
          const expiresAtDate = new Date(Date.now() + options.expiryHours * 60 * 60 * 1000);
          formData.append("expiresAt", expiresAtDate.toISOString());
          formData.append("expiryHours", options.expiryHours.toString());
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
            reject(new Error(xhr.responseText || `Upload failed with status code ${xhr.status}`));
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
   * Fetches metadata for a single file by code
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
      if (metadata.maxDownloads !== undefined && metadata.downloadCount >= metadata.maxDownloads) {
        this.deleteMockData(code);
        throw new Error("This file has reached its download limit.");
      }

      return metadata;
    } else {
      // Fetch the binary file directly since the backend does not have a separate metadata endpoint
      const response = await fetch(`${API_BASE_URL}/files/${code}`);
      if (!response.ok) {
        if (response.status === 404) {
          throw new Error("File not found or has expired.");
        }
        if (response.status === 410 || response.status === 500) {
          throw new Error("This file has expired or is unavailable.");
        }
        throw new Error(`Failed to fetch file: ${response.statusText}`);
      }

      const blob = await response.blob();
      
      // Parse original filename from Content-Disposition header
      let originalFileName = "shared-file";
      const contentDisposition = response.headers.get("content-disposition");
      if (contentDisposition) {
        const filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
        const matches = filenameRegex.exec(contentDisposition);
        if (matches != null && matches[1]) {
          originalFileName = matches[1].replace(/['"]/g, "");
        }
      }

      const mimeType = blob.type || response.headers.get("content-type") || "application/octet-stream";
      const sizeBytes = blob.size;

      const metadata: FileMetadata = {
        code,
        originalFileName,
        mimeType,
        sizeBytes,
        downloadCount: 0,
        createdAt: new Date().toISOString(),
      };

      // Merge local uploader history metadata if present
      const localHistory = historyService.getHistory();
      const matchedLocal = localHistory.find((item) => item.code === code);
      if (matchedLocal) {
        metadata.expiresAt = matchedLocal.expiresAt;
        metadata.maxDownloads = matchedLocal.maxDownloads;
        metadata.downloadCount = matchedLocal.downloadCount;
        metadata.createdAt = matchedLocal.createdAt;
      }

      return metadata;
    }
  },

  /**
   * Deletes a file by code
   */
  async deleteFile(code: string): Promise<void> {
    if (isMockMode()) {
      await delay(300);
      this.deleteMockData(code);
    } else {
      const response = await fetch(`${API_BASE_URL}/files/${code}`, {
        method: "DELETE",
      });
      if (!response.ok) {
        throw new Error(`Failed to delete file: ${response.statusText}`);
      }
      historyService.removeFromHistory(code);
    }
  },

  /**
   * Generates the API download URL for direct access or triggers download in Mock mode
   */
  getDownloadUrl(code: string): string {
    if (isMockMode()) {
      return "#mock-download";
    }
    return `${API_BASE_URL}/files/${code}`;
  },

  /**
   * Downloads a file and increments count (handles mock downloads directly)
   */
  async downloadFile(code: string, fileName: string): Promise<void> {
    if (isMockMode()) {
      const content = localStorage.getItem(`mock_file_content_${code}`);
      const metaStr = localStorage.getItem(`mock_file_meta_${code}`);
      
      if (!metaStr) {
        throw new Error("File not found.");
      }
      
      const metadata = JSON.parse(metaStr) as FileMetadata;
      
      // Increment download count
      metadata.downloadCount++;
      
      // Save updated count
      localStorage.setItem(`mock_file_meta_${code}`, JSON.stringify(metadata));
      historyService.updateDownloadCount(code);

      // Check if limits exceeded, delete after this download if needed
      let exceeded = false;
      if (metadata.maxDownloads !== undefined && metadata.downloadCount >= metadata.maxDownloads) {
        exceeded = true;
      }

      // Trigger download in browser
      const dataUrl = content || `data:text/plain;base64,${btoa("Placeholder content for downloaded file")}`;
      const link = document.createElement("a");
      link.href = dataUrl;
      link.download = fileName;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);

      if (exceeded) {
        await delay(500);
        this.deleteMockData(code);
      }
    } else {
      // In real mode, download via dynamic anchor tag
      const downloadUrl = this.getDownloadUrl(code);
      const link = document.createElement("a");
      link.href = downloadUrl;
      // Triggers browser download dialog
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      
      setTimeout(() => {
        historyService.updateDownloadCount(code);
      }, 1000);
    }
  },

  /**
   * Helper to clean up mock storage keys
   */
  deleteMockData(code: string) {
    localStorage.removeItem(`mock_file_meta_${code}`);
    localStorage.removeItem(`mock_file_content_${code}`);
    historyService.removeFromHistory(code);
  },
};
