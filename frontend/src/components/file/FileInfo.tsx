import * as React from "react";
import { FileMetadata } from "../../types/file";
import "./FileInfo.css";

interface FileInfoProps {
  metadata: FileMetadata;
}

const formatBytes = (bytes: number): string => {
  if (bytes === 0) return "0 B";
  const k = 1024;
  const sizes = ["B", "KB", "MB"];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + " " + sizes[i];
};

const getExtension = (name: string): string => {
  return name.split(".").pop()?.toUpperCase() || "FILE";
};

const formatExpiry = (expiresAt: string): { text: string; expired: boolean } => {
  const diff = new Date(expiresAt).getTime() - Date.now();
  if (diff <= 0) return { text: "Expired", expired: true };

  const hours = Math.floor(diff / (1000 * 60 * 60));
  if (hours > 24) {
    return { text: `Expires in ${Math.floor(hours / 24)} days`, expired: false };
  }
  if (hours > 0) {
    return { text: `Expires in ${hours} hours`, expired: false };
  }
  const mins = Math.floor(diff / (1000 * 60));
  return { text: `Expires in ${mins} minutes`, expired: false };
};

export const FileInfo: React.FC<FileInfoProps> = ({ metadata }) => {
  const parts: React.ReactNode[] = [
    formatBytes(metadata.sizeBytes),
    getExtension(metadata.originalFileName),
  ];

  if (metadata.maxDownloads) {
    parts.push(`${metadata.downloadCount} of ${metadata.maxDownloads} downloads`);
  } else if (metadata.downloadCount > 0) {
    parts.push(`${metadata.downloadCount} downloads`);
  }

  if (metadata.expiresAt) {
    const expiry = formatExpiry(metadata.expiresAt);
    parts.push(
      <span key="expiry" className={expiry.expired ? "portal-meta--expired" : undefined}>
        {expiry.text}
      </span>
    );
  }

  return (
    <div className="portal-meta" aria-label="File details">
      {metadata.isEncrypted && (
        <>
          <span className="portal-meta-badge">Encrypted</span>
          <span className="portal-meta-sep">·</span>
        </>
      )}
      {parts.map((part, i) => (
        <React.Fragment key={i}>
          {i > 0 && <span className="portal-meta-sep">·</span>}
          {part}
        </React.Fragment>
      ))}
    </div>
  );
};
