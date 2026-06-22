import * as React from "react";
import { Link } from "react-router-dom";
import { fileService } from "../../services/file.service";

interface QueueItem {
  id: string;
  fileName: string;
  sizeBytes: number;
  progress: number;
  status: "uploading" | "completed" | "error";
  errorMessage?: string;
  mimeType: string;
  code?: string;
}

const UploadForm: React.FC = () => {
  const [uploads, setUploads] = React.useState<QueueItem[]>([]);
  const [dragActive, setDragActive] = React.useState(false);
  const [maxDownloads, setMaxDownloads] = React.useState<number>(0); // 0 = unlimited
  const [expiryHours, setExpiryHours] = React.useState<number>(24);   // 24 = 1 day
  const [toastMsg, setToastMsg] = React.useState<string | null>(null);

  const fileInputRef = React.useRef<HTMLInputElement>(null);

  const showToast = (msg: string) => {
    setToastMsg(msg);
    setTimeout(() => setToastMsg(null), 3000);
  };

  const handleDrag = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === "dragenter" || e.type === "dragover") {
      setDragActive(true);
    } else if (e.type === "dragleave") {
      setDragActive(false);
    }
  };

  const processFile = async (file: File) => {
    const id = Math.random().toString(36).substring(2, 9);
    
    // Create initial upload queue item
    const newItem: QueueItem = {
      id,
      fileName: file.name,
      sizeBytes: file.size,
      progress: 0,
      status: "uploading",
      mimeType: file.type || "application/octet-stream",
    };

    setUploads((prev) => [newItem, ...prev]);

    // Validation checks
    const MAX_SIZE = 10 * 1024 * 1024; // 10MB
    if (file.size > MAX_SIZE) {
      setUploads((prev) =>
        prev.map((item) =>
          item.id === id
            ? { ...item, status: "error", errorMessage: "file size is too large", progress: 0 }
            : item
        )
      );
      return;
    }

    try {
      const uploadOptions = {
        maxDownloads: maxDownloads > 0 ? maxDownloads : undefined,
        expiryHours: expiryHours > 0 ? expiryHours : undefined,
      };

      const meta = await fileService.uploadFile(file, uploadOptions, (percent) => {
        // Dynamic progress callback
        setUploads((prev) =>
          prev.map((item) =>
            item.id === id ? { ...item, progress: percent } : item
          )
        );
      });

      // Mark completed & set code
      setUploads((prev) =>
        prev.map((item) =>
          item.id === id
            ? { ...item, status: "completed", progress: 100, code: meta.code }
            : item
        )
      );

      // Auto-copy shareable link
      const shareLink = `${window.location.origin}/f/${meta.code}`;
      navigator.clipboard.writeText(shareLink).then(() => {
        showToast(`Copied link for ${file.name}!`);
      });

    } catch (err: any) {
      setUploads((prev) =>
        prev.map((item) =>
          item.id === id
            ? { ...item, status: "error", errorMessage: err?.message || "Upload failed", progress: 0 }
            : item
        )
      );
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);
    
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      processFile(e.dataTransfer.files[0]);
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      processFile(e.target.files[0]);
    }
  };

  const onButtonClick = () => {
    fileInputRef.current?.click();
  };

  const handleCancelItem = (id: string) => {
    // Remove selected item from active queue list
    setUploads((prev) => prev.filter((item) => item.id !== id));
  };

  const copyItemLink = (code: string, e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    const shareLink = `${window.location.origin}/f/${code}`;
    navigator.clipboard.writeText(shareLink).then(() => {
      showToast("Link copied to clipboard!");
    });
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
    <div className="dashboard-card">
      {toastMsg && <div className="toast">{toastMsg}</div>}

      <div className="dashboard-title-group">
        <h1 className="dashboard-title">Upload File</h1>
        <p className="dashboard-subtitle">Upload documents you want to share with your team</p>
      </div>

      <div className="dashboard-grid">
        {/* Left Side: Drag Zone and upload options */}
        <div className="dashboard-left">
          <div 
            className={`dropzone ${dragActive ? "active" : ""}`}
            onDragEnter={handleDrag}
            onDragLeave={handleDrag}
            onDragOver={handleDrag}
            onDrop={handleDrop}
            onClick={onButtonClick}
            style={{ marginBottom: "20px" }}
          >
            <input 
              type="file" 
              ref={fileInputRef}
              onChange={handleFileChange}
              style={{ display: "none" }}
            />
            
            {/* Minimalist Upload Cloud-Arrow icon */}
            <svg 
              width="50" 
              height="50" 
              viewBox="0 0 24 24" 
              fill="none" 
              stroke="#818cf8" 
              strokeWidth="1.5" 
              strokeLinecap="round" 
              strokeLinejoin="round"
              style={{ marginBottom: "16px" }}
            >
              <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
              <polyline points="17 8 12 3 7 8" />
              <line x1="12" y1="3" x2="12" y2="15" />
            </svg>

            <div className="dropzone-text" style={{ fontSize: "15px", color: "var(--text)" }}>
              Drag and drop file here
            </div>
            <div className="dropzone-subtext" style={{ margin: "8px 0", color: "#a5b4fc" }}>
              -OR-
            </div>
            <button 
              type="button" 
              className="btn btn-primary"
              style={{ background: "#1e0094", borderRadius: "10px", padding: "10px 24px", fontSize: "14px" }}
              onClick={(e) => { e.stopPropagation(); onButtonClick(); }}
            >
              Browse Files
            </button>
          </div>

          <div className="form-group" style={{ marginBottom: "12px" }}>
            <label className="form-label" style={{ fontSize: "13px", color: "#1e1b4b" }} htmlFor="expiry-select">Expiry Duration</label>
            <select 
              id="expiry-select"
              className="form-select"
              value={expiryHours}
              onChange={(e) => setExpiryHours(Number(e.target.value))}
              style={{ padding: "8px 12px", fontSize: "14px" }}
            >
              <option value={1}>1 Hour</option>
              <option value={24}>1 Day</option>
              <option value={168}>7 Days</option>
              <option value={0}>No Expiry</option>
            </select>
          </div>

          <div className="form-group">
            <label className="form-label" style={{ fontSize: "13px", color: "#1e1b4b" }} htmlFor="downloads-select">Download Limit</label>
            <select 
              id="downloads-select"
              className="form-select"
              value={maxDownloads}
              onChange={(e) => setMaxDownloads(Number(e.target.value))}
              style={{ padding: "8px 12px", fontSize: "14px" }}
            >
              <option value={0}>Unlimited downloads</option>
              <option value={1}>1 download only</option>
              <option value={5}>5 downloads</option>
              <option value={10}>10 downloads</option>
            </select>
          </div>
        </div>

        {/* Right Side: Dynamic Session Upload Queue */}
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
                        {item.status === "error" ? item.errorMessage : `${item.fileName.split('.')[0]}(${item.progress}%)`}
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
                            onClick={(e) => copyItemLink(item.code!, e)}
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
                          onClick={() => handleCancelItem(item.id)}
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
      </div>
    </div>
  );
};

export default UploadForm;