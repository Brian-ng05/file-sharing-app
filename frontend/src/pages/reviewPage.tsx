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
          // Point directly to real download URL
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
      
      // Snappily increment download count locally to reflect change immediately
      setMetadata((prev) => {
        if (!prev) return null;
        const newCount = prev.downloadCount + 1;
        
        // If we hit limit in mock mode, redirect after a short latency
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

  // Helper formatting size
  const formatBytes = (bytes: number): string => {
    if (bytes === 0) return "0 B";
    const k = 1024;
    const sizes = ["B", "KB", "MB"];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + " " + sizes[i];
  };

  // Check if file is image for previewing
  const isImage = (mime: string): boolean => {
    const imgTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];
    return imgTypes.includes(mime.toLowerCase()) || mime.toLowerCase().startsWith("image/");
  };

  // Icon type generator based on MIME type
  const getFileIcon = (mime: string): string => {
    const lower = mime.toLowerCase();
    if (lower.includes("pdf")) return "📄";
    if (lower.includes("zip") || lower.includes("rar") || lower.includes("tar") || lower.includes("gz")) return "📦";
    if (lower.includes("audio") || lower.includes("mp3") || lower.includes("wav")) return "🎵";
    if (lower.includes("video") || lower.includes("mp4") || lower.includes("avi")) return "🎥";
    if (lower.includes("text") || lower.includes("plain") || lower.includes("json") || lower.includes("javascript")) return "📝";
    return "📁";
  };

  if (loading) {
    return (
      <div className="card" style={{ display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", minHeight: "300px" }}>
        <div style={{ fontSize: "36px", animation: "spin 1.5s linear infinite" }}>🔄</div>
        <p style={{ marginTop: "16px", color: "var(--text)" }}>Retrieving file metadata...</p>
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
      <div className="card" style={{ textAlign: "center", padding: "48px 32px" }}>
        <div style={{ fontSize: "56px", marginBottom: "20px" }}>⚠️</div>
        <h2 style={{ color: "var(--text-h)" }}>Access Denied / Expired</h2>
        <p style={{ color: "var(--text)", marginBottom: "32px", fontSize: "15px" }}>
          {error || "The file you are trying to view is no longer available."}
        </p>
        <div className="action-row">
          <Link to="/" className="btn btn-primary">
            Upload a File
          </Link>
          <Link to="/history" className="btn btn-secondary">
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

      <div className="preview-card">
        <h2 style={{ marginBottom: "20px", textOverflow: "ellipsis", overflow: "hidden", whiteSpace: "nowrap" }}>
          {metadata.originalFileName}
        </h2>

        {/* Display Preview */}
        <div className="preview-display">
          {fileIsImage && previewUrl ? (
            <img 
              src={previewUrl} 
              alt={metadata.originalFileName} 
              className="preview-image"
              onError={() => setPreviewUrl(null)} // fallback if load fails
            />
          ) : (
            <div className="preview-icon-wrapper">
              <span className="file-icon" style={{ fontSize: "72px" }}>
                {getFileIcon(metadata.mimeType)}
              </span>
              <p style={{ fontSize: "14px", color: "var(--text)" }}>
                No inline preview available for this file type.
              </p>
            </div>
          )}
        </div>

        {/* Metadata Details */}
        <div className="preview-details">
          <div className="preview-row">
            <span className="preview-label">File Code</span>
            <span className="preview-value"><code>{metadata.code}</code></span>
          </div>
          <div className="preview-row">
            <span className="preview-label">File Size</span>
            <span className="preview-value">{formatBytes(metadata.sizeBytes)}</span>
          </div>
          <div className="preview-row">
            <span className="preview-label">MIME Type</span>
            <span className="preview-value" style={{ fontSize: "12px" }}>{metadata.mimeType}</span>
          </div>
          <div className="preview-row">
            <span className="preview-label">Downloads Limit</span>
            <span className="preview-value">
              {metadata.maxDownloads 
                ? `${metadata.downloadCount} / ${metadata.maxDownloads} downloads`
                : `${metadata.downloadCount} / Unlimited`
              }
            </span>
          </div>
          <div className="preview-row">
            <span className="preview-label">Uploaded At</span>
            <span className="preview-value">
              {new Date(metadata.createdAt).toLocaleString()}
            </span>
          </div>
          {metadata.expiresAt && (
            <div className="preview-row">
              <span className="preview-label">Expires At</span>
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
            style={{ minWidth: "150px" }}
          >
            Download File
          </button>
          
          <button 
            type="button" 
            className="btn btn-danger" 
            onClick={handleDelete}
          >
            Delete File
          </button>

          <Link to="/" className="btn btn-secondary">
            Upload Another
          </Link>
        </div>
      </div>
    </div>
  );
};

export default ReviewPage;