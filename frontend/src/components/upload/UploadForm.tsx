import * as React from "react";
import { Link } from "react-router-dom";
import { fileService } from "../../services/file.service";
import { FileMetadata } from "../../types/file";

const UploadForm: React.FC = () => {
  const [file, setFile] = React.useState<File | null>(null);
  const [dragActive, setDragActive] = React.useState(false);
  const [maxDownloads, setMaxDownloads] = React.useState<number>(0); // 0 = unlimited
  const [expiryHours, setExpiryHours] = React.useState<number>(24);   // 24 = 1 day
  const [isUploading, setIsUploading] = React.useState(false);
  const [progress, setProgress] = React.useState(0);
  const [error, setError] = React.useState<string | null>(null);
  const [result, setResult] = React.useState<FileMetadata | null>(null);
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

  const validateAndSetFile = (selectedFile: File) => {
    const MAX_SIZE = 10 * 1024 * 1024; // 10MB
    if (selectedFile.size > MAX_SIZE) {
      setError(`File is too large (${(selectedFile.size / (1024 * 1024)).toFixed(2)} MB). Maximum allowed size is 10 MB.`);
      setFile(null);
      return;
    }
    setError(null);
    setFile(selectedFile);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);
    
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      validateAndSetFile(e.dataTransfer.files[0]);
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      validateAndSetFile(e.target.files[0]);
    }
  };

  const onButtonClick = () => {
    fileInputRef.current?.click();
  };

  const handleUpload = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!file) return;

    setIsUploading(true);
    setProgress(0);
    setError(null);

    // Simulate progress uploading
    const interval = setInterval(() => {
      setProgress((prev) => {
        if (prev >= 90) {
          clearInterval(interval);
          return 90;
        }
        return prev + 10;
      });
    }, 80);

    try {
      const uploadOptions = {
        maxDownloads: maxDownloads > 0 ? maxDownloads : undefined,
        expiryHours: expiryHours > 0 ? expiryHours : undefined,
      };

      const meta = await fileService.uploadFile(file, uploadOptions);
      
      clearInterval(interval);
      setProgress(100);
      
      // Short delay for progress visual
      setTimeout(() => {
        setResult(meta);
        setIsUploading(false);
        
        // Auto copy to clipboard
        const shareLink = `${window.location.origin}/f/${meta.code}`;
        navigator.clipboard.writeText(shareLink).then(() => {
          showToast("File uploaded! Share link copied to clipboard.");
        }).catch(() => {
          showToast("File uploaded successfully!");
        });
      }, 300);
      
    } catch (err: any) {
      clearInterval(interval);
      setIsUploading(false);
      setError(err?.message || "An error occurred during file upload.");
    }
  };

  const handleReset = () => {
    setFile(null);
    setResult(null);
    setProgress(0);
    setError(null);
  };

  const copyLinkToClipboard = () => {
    if (!result) return;
    const shareLink = `${window.location.origin}/f/${result.code}`;
    navigator.clipboard.writeText(shareLink).then(() => {
      showToast("Link copied to clipboard!");
    });
  };

  // Helper for size display
  const formatBytes = (bytes: number): string => {
    if (bytes === 0) return "0 Bytes";
    const k = 1024;
    const sizes = ["Bytes", "KB", "MB"];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + " " + sizes[i];
  };

  return (
    <div className="card">
      {toastMsg && <div className="toast">{toastMsg}</div>}

      {!result ? (
        <form onSubmit={handleUpload}>
          <h2 style={{ marginBottom: "16px" }}>Upload a File</h2>
          
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
            <div className="dropzone-icon">🚀</div>
            {file ? (
              <>
                <div className="dropzone-text" style={{ color: "var(--accent)" }}>
                  {file.name}
                </div>
                <div className="dropzone-subtext">
                  File size: {formatBytes(file.size)}
                </div>
              </>
            ) : (
              <>
                <div className="dropzone-text">Drag & drop your file here</div>
                <div className="dropzone-subtext">or click to browse from device</div>
                <div className="dropzone-subtext" style={{ marginTop: "8px", fontSize: "11px" }}>
                  Maximum file size: 10 MB
                </div>
              </>
            )}
          </div>

          {error && (
            <div style={{ color: "rgb(239, 68, 68)", fontSize: "14px", marginBottom: "15px", fontWeight: 500 }}>
              ⚠️ {error}
            </div>
          )}

          {isUploading && (
            <div style={{ marginBottom: "20px" }}>
              <div style={{ display: "flex", justifyContent: "space-between", fontSize: "14px", fontWeight: 500 }}>
                <span>Uploading file...</span>
                <span>{progress}%</span>
              </div>
              <div className="progress-container">
                <div className="progress-bar" style={{ width: `${progress}%` }} />
              </div>
            </div>
          )}

          {!isUploading && file && (
            <>
              <div className="form-group">
                <label className="form-label" htmlFor="expiry-select">Expiry Duration</label>
                <select 
                  id="expiry-select"
                  className="form-select"
                  value={expiryHours}
                  onChange={(e) => setExpiryHours(Number(e.target.value))}
                >
                  <option value={1}>1 Hour</option>
                  <option value={24}>1 Day (Default)</option>
                  <option value={168}>7 Days</option>
                  <option value={0}>No Expiry (Keep forever)</option>
                </select>
              </div>

              <div className="form-group" style={{ marginBottom: "24px" }}>
                <label className="form-label" htmlFor="downloads-select">Download Limit</label>
                <select 
                  id="downloads-select"
                  className="form-select"
                  value={maxDownloads}
                  onChange={(e) => setMaxDownloads(Number(e.target.value))}
                >
                  <option value={0}>Unlimited downloads</option>
                  <option value={1}>1 download only</option>
                  <option value={5}>5 downloads</option>
                  <option value={10}>10 downloads</option>
                  <option value={50}>50 downloads</option>
                </select>
              </div>

              <button type="submit" className="btn btn-primary" style={{ width: "100%" }}>
                Upload & Share
              </button>
            </>
          )}
        </form>
      ) : (
        <div style={{ textAlign: "center" }}>
          <div style={{ fontSize: "48px", marginBottom: "12px" }}>🎉</div>
          <h2>Upload Completed!</h2>
          <p style={{ fontSize: "14px", color: "var(--text)", marginBottom: "24px" }}>
            Your file is now ready for sharing.
          </p>

          <div style={{ background: "var(--code-bg)", padding: "16px", borderRadius: "8px", textAlign: "left", marginBottom: "24px" }}>
            <div style={{ fontSize: "14px", fontWeight: "bold", color: "var(--text-h)", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
              {result.originalFileName}
            </div>
            <div style={{ fontSize: "12px", color: "var(--text)", marginTop: "4px" }}>
              Size: {formatBytes(result.sizeBytes)} &bull; Code: <code>{result.code}</code>
            </div>
          </div>

          <div className="form-group">
            <label className="form-label">Shareable Link</label>
            <div style={{ display: "flex", gap: "8px" }}>
              <input 
                type="text" 
                className="form-input" 
                readOnly 
                value={`${window.location.origin}/f/${result.code}`}
                onClick={(e) => (e.target as HTMLInputElement).select()}
              />
              <button type="button" className="btn btn-secondary" onClick={copyLinkToClipboard}>
                Copy
              </button>
            </div>
          </div>

          <div className="action-row" style={{ marginTop: "24px" }}>
            <Link to={`/f/${result.code}`} className="btn btn-primary">
              View Preview
            </Link>
            <button type="button" className="btn btn-secondary" onClick={handleReset}>
              Upload Another
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default UploadForm;