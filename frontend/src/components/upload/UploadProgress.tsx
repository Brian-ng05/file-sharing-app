import * as React from "react";
import { Link } from "react-router-dom";

export interface QueueItem {
  id: string;
  fileName: string;
  sizeBytes: number;
  progress: number;
  status: "uploading" | "completed" | "error";
  errorMessage?: string;
  mimeType: string;
  code?: string;
}

interface UploadProgressProps {
  uploads: QueueItem[];
  onCancel: (id: string) => void;
  onCopyLink: (code: string, e: React.MouseEvent) => void;
}

const getFileBadgeClass = (name: string): string => {
  const ext = name.split(".").pop()?.toLowerCase() || "";
  if (ext === "pdf") return "pdf";
  if (ext === "svg") return "svg";
  if (ext === "eps") return "eps";
  if (["png", "jpg", "jpeg", "gif", "webp"].includes(ext)) return "img";
  if (["zip", "rar", "tar", "gz", "7z"].includes(ext)) return "zip";
  if (["doc", "docx", "xls", "xlsx", "txt"].includes(ext)) return "doc";
  return "oth";
};

const getFileBadgeLabel = (name: string): string => {
  const ext = name.split(".").pop()?.toLowerCase() || "file";
  return ext.slice(0, 3).toUpperCase();
};

export const UploadProgress: React.FC<UploadProgressProps> = ({
  uploads,
  onCancel,
  onCopyLink,
}) => {
  return (
    <div className="dashboard-right">
      <h3 className="queue-title">Upload Files</h3>
      
      {uploads.length === 0 ? (
        <div className="queue-empty">
          <div style={{ fontSize: "28px", marginBottom: "8px" }}>📥</div>
          <p style={{ margin: 0, fontWeight: 500 }}>No uploads in this session</p>
          <p style={{ margin: "4px 0 0", fontSize: "12px", color: "var(--text)" }}>
            Drag files onto the dropzone to start sharing
          </p>
        </div>
      ) : (
        <div className="queue-list">
          {uploads.map((item) => (
            <div 
              key={item.id} 
              className={`queue-item ${item.status === "error" ? "error" : ""}`}
            >
              {/* Extension Logo Badge */}
              <div className={`file-badge ${getFileBadgeClass(item.fileName)}`}>
                {getFileBadgeLabel(item.fileName)}
              </div>

              <div className="queue-item-content">
                <div className="queue-item-header">
                  {/* Name or error status */}
                  <span className="queue-filename" title={item.fileName}>
                    {item.status === "error" ? item.errorMessage : `${item.fileName.split('.')[0]} (${item.progress}%)`}
                  </span>

                  {/* Status / Copy Trigger */}
                  {item.status === "completed" && item.code ? (
                    <div style={{ display: "flex", gap: "6px", alignItems: "center" }}>
                      <Link 
                        to={`/f/${item.code}`}
                        className="queue-status completed"
                        style={{ textDecoration: "none" }}
                        title="Open Preview Page"
                      >
                        Completed
                      </Link>
                      <button 
                        type="button" 
                        onClick={(e) => onCopyLink(item.code!, e)}
                        title="Copy Share Link"
                        style={{ background: "none", border: "none", cursor: "pointer", padding: 0 }}
                      >
                        📋
                      </button>
                    </div>
                  ) : (
                    <button 
                      type="button"
                      className="queue-status cancel-btn"
                      onClick={() => onCancel(item.id)}
                    >
                      Cancel
                    </button>
                  )}
                </div>

                {/* Progress Bar Line */}
                <div className="queue-progress-container">
                  <div 
                    className="queue-progress-bar" 
                    style={{ width: `${item.status === "error" ? 100 : item.progress}%` }}
                  />
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
