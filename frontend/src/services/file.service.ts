import { FileMetadata, UploadOptions, FileErrorType } from "../types/file";
import { API_BASE_URL } from "./api";
import { historyService } from "./history.service";

// Max file size allowed (10 MB)
const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10,485,760 bytes

/**
 * Error class that carries a FileErrorType so the UI can render distinct states.
 */
export class FileError extends Error {
  type: FileErrorType;
  constructor(type: FileErrorType, message: string) {
    super(message);
    this.name = "FileError";
    this.type = type;
  }
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
  },

  /**
   * Fetches metadata for a file by code.
   * Calls GET /files/{code}/info (does NOT increment download count).
   */
  async getFileMetadata(code: string): Promise<FileMetadata> {
    // Real API: call GET /files/{code}/info
    let response: Response;
    try {
      response = await fetch(`${API_BASE_URL}/files/${encodeURIComponent(code)}/info`);
    } catch (err) {
      throw new FileError("network_error", "Cannot connect to the server. Please make sure the backend services are running.");
    }

    if (!response.ok) {
      if (response.status === 404) {
        throw new FileError("not_found", "This link may be incorrect or the file was deleted.");
      }
      if (response.status === 410) {
        throw new FileError("expired", "This file is no longer available because its expiry time has passed.");
      }
      if (response.status === 403) {
        throw new FileError("download_limit", "This file has reached its maximum number of downloads.");
      }
      if (response.status === 502 || response.status === 503) {
        throw new FileError("server_error", "The server could not load this file right now. Please try again later.");
      }
      throw new FileError("server_error", `The server returned an error (${response.status}). Please try again later.`);
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
  },

  /**
   * Verifies a file's password on the server.
   */
  async verifyPassword(code: string, password: string): Promise<boolean> {
    let response: Response;
    try {
      response = await fetch(
        `${API_BASE_URL}/files/${encodeURIComponent(code)}/verify-password`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ password }),
        }
      );
    } catch (err) {
      throw new FileError("network_error", "Cannot connect to the server.");
    }

    if (response.status === 404) {
      throw new FileError("not_found", "File not found.");
    }
    if (response.status === 401 || response.status === 403) {
      throw new FileError("invalid_password", "Incorrect password. Please try again.");
    }
    if (!response.ok) {
      throw new FileError("server_error", "Failed to verify password. Please try again.");
    }

    const data = await response.json() as { valid: boolean };
    if (!data.valid) {
      throw new FileError("invalid_password", "Incorrect password. Please try again.");
    }
    return true;
  },

  /**
   * Deletes a file by code.
   */
  async deleteFile(code: string): Promise<void> {
    let response: Response;
    try {
      response = await fetch(`${API_BASE_URL}/files/${encodeURIComponent(code)}`, {
        method: "DELETE",
      });
    } catch (err) {
      throw new FileError("network_error", "Cannot connect to the server.");
    }
    if (!response.ok) {
      if (response.status === 404) {
        throw new FileError("not_found", "File not found.");
      }
      throw new FileError("server_error", `Failed to delete file: ${response.statusText}`);
    }
    historyService.removeFromHistory(code);
  },

  /**
   * Generates the API download URL.
   */
  getDownloadUrl(code: string): string {
    return `${API_BASE_URL}/files/${encodeURIComponent(code)}`;
  },

  /**
   * Downloads a file using direct anchor navigation.
   * This avoids S3 CORS issues that occur with fetch + blob,
   * at the cost of not being able to catch 410/403 errors in-app.
   */
  async downloadFile(code: string, _fileName: string, password?: string): Promise<void> {
    let url = this.getDownloadUrl(code);
    if (password) {
      url += `?password=${encodeURIComponent(password)}`;
    }

    const link = document.createElement("a");
    link.href = url;
    link.download = _fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  },
};
