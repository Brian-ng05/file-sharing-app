import * as React from "react";
import { Link } from "react-router-dom";
import { historyService } from "../services/history.service";
import { fileService } from "../services/file.service";
import { FileMetadata } from "../types/file";

const HistoryPage: React.FC = () => {
  const [historyList, setHistoryList] = React.useState<FileMetadata[]>([]);
  const [toastMsg, setToastMsg] = React.useState<string | null>(null);

  React.useEffect(() => {
    // Read local history on mount
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

  // Helper formatting size
  const formatBytes = (bytes: number): string => {
    if (bytes === 0) return "0 B";
    const k = 1024;
    const sizes = ["B", "KB", "MB"];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + " " + sizes[i];
  };

  // Helper formatting expiry
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

  return (
    <div style={{ width: "100%", display: "flex", flexDirection: "column", alignItems: "center" }}>
      {toastMsg && <div className="toast">{toastMsg}</div>}
      
      <h1 style={{ fontSize: "36px", marginBottom: "8px" }}>Upload History</h1>
      <p style={{ color: "var(--text)", marginBottom: "32px" }}>
        List of files you've uploaded from this browser.
      </p>

      {historyList.length === 0 ? (
        <div className="card" style={{ textAlign: "center", padding: "48px 32px" }}>
          <div style={{ fontSize: "48px", marginBottom: "16px" }}>📁</div>
          <h3>No uploads found</h3>
          <p style={{ color: "var(--text)", marginBottom: "24px", fontSize: "14px" }}>
            You haven't uploaded any files yet, or they have all expired.
          </p>
          <Link to="/" className="btn btn-primary">
            Upload a File
          </Link>
        </div>
      ) : (
        <div className="history-container">
          <div className="history-header">
            <h3 className="history-title">My Shared Files</h3>
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
                      <div className="file-name-cell" title={file.originalFileName}>
                        {file.originalFileName}
                      </div>
                      <div style={{ fontSize: "11px", color: "var(--text)", marginTop: "2px" }}>
                        Code: <code>{file.code}</code>
                      </div>
                    </td>
                    <td>{formatBytes(file.sizeBytes)}</td>
                    <td>
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
                          onClick={() => copyLink(file.code)}
                          title="Copy Shareable Link"
                        >
                          Copy Link
                        </button>
                        <Link 
                          to={`/f/${file.code}`} 
                          className="btn btn-primary btn-sm"
                          title="Preview File"
                        >
                          Preview
                        </Link>
                        <button 
                          type="button" 
                          className="btn btn-danger btn-sm" 
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