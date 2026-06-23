export interface FileMetadata {
  code: string;
  originalFileName: string;
  mimeType: string;
  sizeBytes: number;
  maxDownloads?: number;
  downloadCount: number;
  expiresAt?: string; // ISO DateTime string
  createdAt: string;  // ISO DateTime string
  isEncrypted?: boolean; // E2EE flag
}

export interface UploadOptions {
  maxDownloads?: number;
  expiryHours?: number; // expiry hours (e.g. 1, 24, 168)
  password?: string;    // E2EE password
}
