import * as React from "react";
import { FileMetadata } from "../../types/file";

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

export const FileInfo: React.FC<FileInfoProps> = ({ metadata }) => {
  return (
    <div className="preview-details" style={{ borderRadius: "12px", background: "var(--social-bg)", border: "1px solid var(--border)", marginBottom: "28px", padding: "8px 0" }}>
      <div className="preview-row" style={{ borderColor: "var(--border)" }}>
        <span className="preview-label" style={{ color: "#1e1b4b", fontWeight: 600 }}>File Code</span>
        <span className="preview-value"><code>{metadata.code}</code></span>
      </div>
      <div className="preview-row" style={{ borderColor: "var(--border)" }}>
        <span className="preview-label" style={{ color: "#1e1b4b", fontWeight: 600 }}>File Size</span>
        <span className="preview-value" style={{ color: "var(--text)" }}>{formatBytes(metadata.sizeBytes)}</span>
      </div>
      <div className="preview-row" style={{ borderColor: "var(--border)" }}>
        <span className="preview-label" style={{ color: "#1e1b4b", fontWeight: 600 }}>MIME Type</span>
        <span className="preview-value" style={{ fontSize: "12px", color: "var(--text)" }}>{metadata.mimeType}</span>
      </div>
      <div className="preview-row" style={{ borderColor: "var(--border)" }}>
        <span className="preview-label" style={{ color: "#1e1b4b", fontWeight: 600 }}>Downloads</span>
        <span className="preview-value" style={{ color: "var(--text)" }}>
          {metadata.maxDownloads 
            ? `${metadata.downloadCount} / ${metadata.maxDownloads} downloads`
            : `${metadata.downloadCount} / Unlimited`
          }
        </span>
      </div>
      <div className="preview-row" style={{ borderColor: "var(--border)", borderBottom: metadata.expiresAt ? "1px solid var(--border)" : "none" }}>
        <span className="preview-label" style={{ color: "#1e1b4b", fontWeight: 600 }}>Uploaded At</span>
        <span className="preview-value" style={{ color: "var(--text)" }}>
          {new Date(metadata.createdAt).toLocaleString()}
        </span>
      </div>
      {metadata.expiresAt && (
        <div className="preview-row" style={{ borderColor: "var(--border)", borderBottom: "none" }}>
          <span className="preview-label" style={{ color: "#1e1b4b", fontWeight: 600 }}>Expires At</span>
          <span className="preview-value" style={{ color: "rgb(239, 68, 68)" }}>
            {new Date(metadata.expiresAt).toLocaleString()}
          </span>
        </div>
      )}
    </div>
  );
};
