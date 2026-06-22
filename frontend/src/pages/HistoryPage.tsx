import * as React from "react";
import { Link } from "react-router-dom";
import { historyService } from "../services/history.service";
import { fileService } from "../services/file.service";
import { FileMetadata } from "../types/file";

const HistoryPage: React.FC = () => {
  const [historyList, setHistoryList] = React.useState<FileMetadata[]>([]);
  const [toastMsg, setToastMsg] = React.useState<string | null>(null);

  React.useEffect(() => {
    setHistoryList(historyService.getHistory());
  }, []);

  const showToast = (msg: string) => {
    setToastMsg(msg);
    setTimeout(() => setToastMsg(null), 3000);
  };

  const copyLink = (code: string) => {
    const link = `${window.location.origin}/f/${code}`;
    navigator.clipboard.writeText(link).then(() => {
      showToast("Link copied to clipboard!");
    });
  };

  const handleDelete = async (code: string) => {
    if (!confirm("Are you sure you want to delete this file from storage?")) {
      return;
    }

    try {
      await fileService.deleteFile(code);
      setHistoryList(historyService.getHistory());
      showToast("File deleted successfully.");
    } catch (err: any) {
      showToast(err?.message || "Failed to delete file.");
    }
  };

  const formatBytes = (bytes: number): string => {
    if (bytes === 0) return "0 B";
    const k = 1024;
    const sizes = ["B", "KB", "MB"];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + " " + sizes[i];
  };

  const formatExpiry = (expiresAt?: string): string => {
    if (!expiresAt) return "Never";
    const diff = new Date(expiresAt).getTime() - Date.now();
    if (diff <= 0) return "Expired";
    
    const hours = Math.floor(diff / (1000 * 60 * 60));
    if (hours > 24) {
      return `${Math.floor(hours / 24)} days left`;
    }
    if (hours > 0) {
      return `${hours} hrs left`;
    }
    const mins = Math.floor(diff / (1000 * 60));
    return `${mins} mins left`;
  };

  // Helper determining extension class for styling badge backgrounds
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

  // Helper converting extension to label (e.g. PDF, SVG, EPS)
  const getFileBadgeLabel = (name: string): string => {
    const ext = name.split(".").pop()?.toLowerCase() || "file";
    return ext.slice(0, 3).toUpperCase();
  };

  return (
    <div style={{ width: "100%", maxWidth: "920px", display: "flex", flexDirection: "column" }}>
      {toastMsg && <div className="toast">{toastMsg}</div>}
      
      <div className="dashboard-title-group" style={{ textAlign: "left" }}>
        <h1 className="dashboard-title">Upload History</h1>
        <p className="dashboard-subtitle">View and manage files you have shared with your team</p>
      </div>

      {historyList.length === 0 ? (
        <div className="card" style={{ textAlign: "center", padding: "48px 32px", marginTop: "24px" }}>
          <div style={{ fontSize: "48px", marginBottom: "16px" }}>📁</div>
          <h3 style={{ color: "#1e1b4b", margin: "0 0 8px" }}>No uploads found</h3>
          <p style={{ color: "var(--text)", marginBottom: "24px", fontSize: "14px" }}>
            You haven't uploaded any files yet, or they have all expired.
          </p>
          <Link 
            to="/" 
            className="btn btn-primary" 
            style={{ background: "#1e0094", borderRadius: "10px", padding: "10px 24px" }}
          >
            Upload a File
          </Link>
        </div>
      ) : (
        <div className="history-container" style={{ marginTop: "24px", borderRadius: "20px" }}>
          <div className="history-header">
            <h3 className="history-title" style={{ color: "#1e1b4b" }}>My Shared Files</h3>
            <span className="mock-badge">{historyList.length} files</span>
          </div>
          
          <div className="history-table-wrapper">
            <table className="history-table">
              <thead>
                <tr>
                  <th>File Name</th>
                  <th>Size</th>
                  <th>Downloads</th>
                  <th>Expires</th>
                  <th style={{ textAlign: "right" }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {historyList.map((file) => (
                  <tr key={file.code}>
                    <td>
                      <div style={{ display: "flex", alignItems: "center", gap: "12px" }}>
                        {/* Extension Logo Badge matching Upload Dashboard */}
                        <div 
                          className={`file-badge ${getFileBadgeClass(file.originalFileName)}`}
                          style={{ width: "32px", height: "38px", fontSize: "8px" }}
                        >
                          {getFileBadgeLabel(file.originalFileName)}
                        </div>
                        <div style={{ minWidth: 0 }}>
                          <div 
                            className="file-name-cell" 
                            title={file.originalFileName}
                            style={{ color: "#1e1b4b", fontSize: "14px", fontWeight: 600, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}
                          >
                            {file.originalFileName}
                          </div>
                          <div style={{ fontSize: "11px", color: "var(--text)", marginTop: "2px" }}>
                            Code: <code>{file.code}</code>
                          </div>
                        </div>
                      </div>
                    </td>
                    <td style={{ color: "var(--text-h)", whiteSpace: "nowrap" }}>{formatBytes(file.sizeBytes)}</td>
                    <td style={{ color: "var(--text-h)", whiteSpace: "nowrap" }}>
                      {file.maxDownloads 
                        ? `${file.downloadCount} / ${file.maxDownloads}`
                        : `${file.downloadCount} / ∞`
                      }
                    </td>
                    <td>
                      <span className={`status-badge ${file.expiresAt ? "expired" : "active"}`}>
                        {formatExpiry(file.expiresAt)}
                      </span>
                    </td>
                    <td>
                      <div style={{ display: "flex", gap: "8px", justifyContent: "flex-end" }}>
                        <button 
                          type="button" 
                          className="btn btn-secondary btn-sm" 
                          style={{ borderRadius: "8px", padding: "6px 12px" }}
                          onClick={() => copyLink(file.code)}
                          title="Copy Shareable Link"
                        >
                          Copy Link
                        </button>
                        <Link 
                          to={`/f/${file.code}`} 
                          className="btn btn-primary btn-sm"
                          style={{ background: "#1e0094", borderRadius: "8px", padding: "6px 12px" }}
                          title="Preview File"
                        >
                          Preview
                        </Link>
                        <button 
                          type="button" 
                          className="btn btn-danger btn-sm" 
                          style={{ borderRadius: "8px", padding: "6px 12px" }}
                          onClick={() => handleDelete(file.code)}
                          title="Delete File"
                        >
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
};

export default HistoryPage;