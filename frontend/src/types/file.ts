export interface FileMetadata {
  code: string;
  originalFileName: string;
  mimeType: string;
  sizeBytes: number;
  requiresPassword: boolean;
  maxDownloads?: number;
  downloadCount?: number;
  expiresAt?: string; // ISO DateTime string
  createdAt: string;  // ISO DateTime string
  thumbnailUrl?: string; // for future use
}

export interface UploadOptions {
  maxDownloads?: number;
  expiryHours?: number; // expiry hours (e.g. 1, 24, 168)
  password?: string;    // server-side BCrypt password
}

export type FileErrorType =
  | "not_found"
  | "expired"
  | "download_limit"
  | "password_required"
  | "invalid_password"
  | "server_error"
  | "network_error";
