import * as React from "react";
import { FileMetadata } from "../../types/file";
import { formatExpiryDisplay } from "../../utils/expiry";
import { useNow } from "../../utils/useNow";
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

export const FileInfo: React.FC<FileInfoProps> = ({ metadata }) => {
  const now = useNow();
  const hasLimit = metadata.maxDownloads !== undefined && metadata.maxDownloads > 0;
  const capped = Math.min(metadata.downloadCount ?? 0, metadata.maxDownloads ?? Infinity);
  const limitReached = hasLimit && capped >= metadata.maxDownloads!;

  const parts: React.ReactNode[] = [
    formatBytes(metadata.sizeBytes),
    getExtension(metadata.originalFileName),
  ];

  if (hasLimit) {
    parts.push(
      <span key="downloads" className={limitReached ? "portal-meta--limit-reached" : undefined}>
        Downloads: {capped}/{metadata.maxDownloads}
      </span>
    );
  } else if ((metadata.downloadCount ?? 0) > 0) {
    parts.push(`Downloads: ${metadata.downloadCount}`);
  } else {
    parts.push("Downloads: Unlimited");
  }

  if (metadata.expiresAt) {
    const expiry = formatExpiryDisplay(metadata.expiresAt, now);
    parts.push(
      <span key="expiry" className={expiry.expired ? "portal-meta--expired" : undefined}>
        {expiry.relativeText}
      </span>
    );
  }

  return (
    <div className="portal-meta" aria-label="File details">
      {metadata.requiresPassword && (
        <>
          <span className="portal-meta-badge">Password Required</span>
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
