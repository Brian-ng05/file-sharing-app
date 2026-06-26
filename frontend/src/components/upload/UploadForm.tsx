import * as React from "react";
import { fileService } from "../../services/file.service";
import { Dropzone } from "./Dropzone";
import { UploadProgress, QueueItem } from "./UploadProgress";

const UploadForm: React.FC = () => {
  const [uploads, setUploads] = React.useState<QueueItem[]>([]);
  const [maxDownloads, setMaxDownloads] = React.useState<number>(0); // 0 = unlimited
  const [expiryHours, setExpiryHours] = React.useState<number>(24);   // 24 = 1 day
  const [password, setPassword] = React.useState<string>("");
  const [toastMsg, setToastMsg] = React.useState<string | null>(null);

  const showToast = (msg: string) => {
    setToastMsg(msg);
    setTimeout(() => setToastMsg(null), 3000);
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

    const allowedMimeTypes = [
      "image/jpeg",
      "image/png",
      "application/pdf",
      "text/plain",
      "application/zip"
    ];
    if (!allowedMimeTypes.includes(file.type)) {
      setUploads((prev) =>
        prev.map((item) =>
          item.id === id
            ? { ...item, status: "error", errorMessage: "invalid file type", progress: 0 }
            : item
        )
      );
      return;
    }

    try {
      const uploadOptions = {
        maxDownloads: maxDownloads > 0 ? maxDownloads : undefined,
        expiryHours: expiryHours > 0 ? expiryHours : undefined,
        password: password ? password : undefined,
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

      // Reset password field
      setPassword("");

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

  const handleCancelItem = (id: string) => {
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

  return (
    <div className="dashboard-card animate-fade-in">
      {toastMsg && <div className="toast">{toastMsg}</div>}

      <div className="dashboard-title-group">
        <h1 className="dashboard-title">Upload File</h1>
        <p className="dashboard-subtitle">Upload documents you want to share with your team</p>
      </div>

      <div className="dashboard-grid">
        {/* Left Side: Drag Zone and upload options */}
        <div className="dashboard-left">
          <Dropzone onFileSelected={processFile} />

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

          <div className="form-group" style={{ marginTop: "12px" }}>
            <label className="form-label" style={{ fontSize: "13px", color: "#1e1b4b" }} htmlFor="password-input">Password Protection (Optional)</label>
            <input 
              id="password-input"
              type="password"
              className="form-input"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Enter password to encrypt file"
              style={{ padding: "8px 12px", fontSize: "14px" }}
            />
          </div>
        </div>

        {/* Right Side: Dynamic Session Upload Queue */}
        <UploadProgress 
          uploads={uploads} 
          onCancel={handleCancelItem} 
          onCopyLink={copyItemLink} 
        />
      </div>
      <style>{`
        .animate-fade-in {
          animation: fadeIn 0.3s ease-out forwards;
        }
        @keyframes fadeIn {
          from { opacity: 0; transform: translateY(10px); }
          to { opacity: 1; transform: translateY(0); }
        }
      `}</style>
    </div>
  );
};

export default UploadForm;