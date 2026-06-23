import * as React from "react";
import { useParams, useNavigate, Link } from "react-router-dom";
import { fileService } from "../services/file.service";
import { FileMetadata } from "../types/file";
import { isMockMode } from "../services/api";

const ReviewPage: React.FC = () => {
  const { code } = useParams<{ code: string }>();
  const navigate = useNavigate();

  const [loading, setLoading] = React.useState(true);
  const [metadata, setMetadata] = React.useState<FileMetadata | null>(null);
  const [previewUrl, setPreviewUrl] = React.useState<string | null>(null);
  const [error, setError] = React.useState<string | null>(null);
  const [toastMsg, setToastMsg] = React.useState<string | null>(null);

  const showToast = (msg: string) => {
    setToastMsg(msg);
    setTimeout(() => setToastMsg(null), 3000);
  };

  React.useEffect(() => {
    if (!code) return;

    const fetchFileData = async () => {
      setLoading(true);
      setError(null);
      try {
        const meta = await fileService.getFileMetadata(code);
        setMetadata(meta);

        // Resolve preview URL
        if (isMockMode()) {
          const content = localStorage.getItem(`mock_file_content_${code}`);
          setPreviewUrl(content);
        } else {
          setPreviewUrl(fileService.getDownloadUrl(code));
        }
      } catch (err: any) {
        setError(err?.message || "File not found or has expired.");
      } finally {
        setLoading(false);
      }
    };

    fetchFileData();
  }, [code]);

  const handleDownload = async () => {
    if (!metadata || !code) return;

    try {
      await fileService.downloadFile(code, metadata.originalFileName);
      showToast("Download started!");
      
      setMetadata((prev) => {
        if (!prev) return null;
        const newCount = prev.downloadCount + 1;
        
        if (prev.maxDownloads !== undefined && newCount >= prev.maxDownloads) {
          setTimeout(() => {
            setError("This file has reached its download limit.");
            setMetadata(null);
          }, 1500);
        }
        
        return {
          ...prev,
          downloadCount: newCount,
        };
      });
    } catch (err: any) {
      showToast(err?.message || "Download failed.");
    }
  };

  const handleDelete = async () => {
    if (!code || !metadata) return;
    if (!confirm("Are you sure you want to delete this file permanently?")) return;

    try {
      await fileService.deleteFile(code);
      showToast("File deleted successfully.");
      setTimeout(() => navigate("/history"), 1000);
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

  const isImage = (mime: string): boolean => {
    const imgTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];
    return imgTypes.includes(mime.toLowerCase()) || mime.toLowerCase().startsWith("image/");
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

  if (loading) {
    return (
      <div 
        className="card" 
        style={{ display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", minHeight: "300px", borderRadius: "20px" }}
      >
        <div style={{ fontSize: "36px", animation: "spin 1.5s linear infinite" }}>🔄</div>
        <p style={{ marginTop: "16px", color: "var(--text)", fontWeight: 500 }}>Retrieving file metadata...</p>
        <style>{`
          @keyframes spin {
            from { transform: rotate(0deg); }
            to { transform: rotate(360deg); }
          }
        `}</style>
      </div>
    );
  }

  if (error || !metadata) {
    return (
      <div className="card" style={{ textAlign: "center", padding: "48px 32px", borderRadius: "20px" }}>
        <div style={{ fontSize: "56px", marginBottom: "20px" }}>⚠️</div>
        <h2 style={{ color: "#1e1b4b", fontWeight: 700 }}>Access Denied / Expired</h2>
        <p style={{ color: "var(--text)", marginBottom: "32px", fontSize: "15px" }}>
          {error || "The file you are trying to view is no longer available."}
        </p>
        <div className="action-row">
          <Link 
            to="/" 
            className="btn btn-primary"
            style={{ background: "#1e0094", borderRadius: "10px", padding: "10px 24px" }}
          >
            Upload a File
          </Link>
          <Link to="/history" className="btn btn-secondary" style={{ borderRadius: "10px", padding: "10px 24px" }}>
            Go to History
          </Link>
        </div>
      </div>
    );
  }

  const fileIsImage = isImage(metadata.mimeType);

  return (
    <div className="preview-container">
      {toastMsg && <div className="toast">{toastMsg}</div>}

      <div className="preview-card" style={{ borderRadius: "20px", padding: "32px" }}>
        <div className="dashboard-title-group" style={{ textAlign: "center", marginBottom: "24px" }}>
          <h2 
            className="dashboard-title" 
            style={{ fontSize: "28px", maxWidth: "100%", textOverflow: "ellipsis", overflow: "hidden", whiteSpace: "nowrap" }}
          >
            {metadata.originalFileName}
          </h2>
          <p className="dashboard-subtitle">Shared Document Overview</p>
        </div>

        {/* Display Preview */}
        <div className="preview-display" style={{ borderRadius: "12px", background: "#f8fafc" }}>
          {fileIsImage && previewUrl ? (
            <img 
              src={previewUrl} 
              alt={metadata.originalFileName} 
              className="preview-image"
              onError={() => setPreviewUrl(null)} // fallback if load fails
            />
          ) : (
            <div className="preview-icon-wrapper" style={{ display: "flex", flexDirection: "column", alignItems: "center" }}>
              {/* Dynamic Badge Icon matching the Dashboard theme */}
              <div 
                className={`file-badge ${getFileBadgeClass(metadata.originalFileName)}`}
                style={{ width: "56px", height: "68px", fontSize: "12px", marginBottom: "16px" }}
              >
                {getFileBadgeLabel(metadata.originalFileName)}
              </div>
              <p style={{ fontSize: "14px", color: "var(--text)", fontWeight: 500 }}>
                No inline preview available for this file type.
              </p>
            </div>
          )}
        </div>

        {/* Metadata Details List */}
        <div className="preview-details" style={{ borderRadius: "12px", background: "var(--social-bg)", border: "1px solid var(--border)", marginBottom: "28px" }}>
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
            <span className="preview-label" style={{ color: "#1e1b4b", fontWeight: 600 }}>Downloads Limit</span>
            <span className="preview-value" style={{ color: "var(--text)" }}>
              {metadata.maxDownloads 
                ? `${metadata.downloadCount} / ${metadata.maxDownloads} downloads`
                : `${metadata.downloadCount} / Unlimited`
              }
            </span>
          </div>
          <div className="preview-row" style={{ borderColor: "var(--border)" }}>
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

        {/* Action Controls */}
        <div className="action-row">
          <button 
            type="button" 
            className="btn btn-primary" 
            onClick={handleDownload}
            style={{ minWidth: "150px", background: "#1e0094", borderRadius: "10px", padding: "12px 24px" }}
          >
            Download File
          </button>
          
          <button 
            type="button" 
            className="btn btn-danger" 
            onClick={handleDelete}
            style={{ borderRadius: "10px", padding: "12px 24px" }}
          >
            Delete File
          </button>

          <Link 
            to="/" 
            className="btn btn-secondary"
            style={{ borderRadius: "10px", padding: "12px 24px" }}
          >
            Upload Another
          </Link>
        </div>
      </div>
    </div>
  );
};

export default ReviewPage;