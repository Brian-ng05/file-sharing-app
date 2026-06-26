import * as React from "react";
import { fileService } from "../services/file.service";
import { Dropzone } from "../components/upload/Dropzone";
import "./UploadPage.css";

type UploadState = "idle" | "selected" | "uploading" | "success";

const FileIcon: React.FC = () => (
  <svg
    className="upload-file-thumb-icon"
    width="20"
    height="20"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.5"
    strokeLinecap="round"
    strokeLinejoin="round"
    aria-hidden="true"
  >
    <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
    <polyline points="14 2 14 8 20 8" />
  </svg>
);

const UploadPage: React.FC = () => {
  const [state, setState] = React.useState<UploadState>("idle");
  const [selectedFile, setSelectedFile] = React.useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = React.useState<string | null>(null);
  const [expiryValue, setExpiryValue] = React.useState<string>("7");
  const [expiryUnit, setExpiryUnit] = React.useState<"days" | "hours" | "minutes">("days");
  const [downloadLimit, setDownloadLimit] = React.useState<string>("");
  const [shareCode, setShareCode] = React.useState<string>("");
  const [toastMsg, setToastMsg] = React.useState<string | null>(null);
  const [error, setError] = React.useState<string | null>(null);

  const showToast = (msg: string) => {
    setToastMsg(msg);
    setTimeout(() => setToastMsg(null), 3000);
  };

  const isImage = (file: File): boolean => {
    return file.type.startsWith("image/");
  };

  const handleFileSelected = (file: File) => {
    const MAX_SIZE = 10 * 1024 * 1024; // 10MB
    if (file.size > MAX_SIZE) {
      setError("File size exceeds 10MB limit");
      return;
    }

    setSelectedFile(file);
    setError(null);

    if (isImage(file)) {
      const url = URL.createObjectURL(file);
      setPreviewUrl(url);
    } else {
      setPreviewUrl(null);
    }

    setState("selected");
  };

  const handleShare = async () => {
    if (!selectedFile) return;

    setState("uploading");
    setError(null);

    try {
      // Calculate expiry hours
      let expiryHours;
      const numExpiryValue = Number(expiryValue);
      if (!isNaN(numExpiryValue) && numExpiryValue > 0) {
        if (expiryUnit === "days") {
          expiryHours = numExpiryValue * 24;
        } else if (expiryUnit === "hours") {
          expiryHours = numExpiryValue;
        } else if (expiryUnit === "minutes") {
          expiryHours = numExpiryValue / 60;
        }
      }

      // Calculate max downloads
      const numDownloadLimit = Number(downloadLimit);
      const maxDownloads = !isNaN(numDownloadLimit) && numDownloadLimit > 0 ? numDownloadLimit : undefined;

      const uploadOptions = {
        maxDownloads,
        expiryHours,
      };

      const meta = await fileService.uploadFile(selectedFile, uploadOptions, (percent) => {
        // Progress callback if needed
      });

      setShareCode(meta.code);
      setState("success");
    } catch (err: any) {
      setError(err?.message || "Upload failed");
      setState("selected");
    }
  };

  const handleCopyLink = () => {
    const shareLink = `${window.location.origin}/f/${shareCode}`;
    navigator.clipboard.writeText(shareLink).then(() => {
      showToast("Link copied to clipboard");
    });
  };

  const handleReset = () => {
    setState("idle");
    setSelectedFile(null);
    setPreviewUrl(null);
    setShareCode("");
    setError(null);
    if (previewUrl) {
      URL.revokeObjectURL(previewUrl);
    }
  };

  const formatBytes = (bytes: number): string => {
    if (bytes === 0) return "0 B";
    const k = 1024;
    const sizes = ["B", "KB", "MB"];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + " " + sizes[i];
  };

  const getFileExtension = (file: File): string => {
    const ext = file.name.split(".").pop()?.toUpperCase() || "FILE";
    return ext.length > 5 ? "FILE" : ext;
  };

  const shareLink = `${window.location.origin}/f/${shareCode}`;

  return (
    <div className={`upload-page ${state === "idle" ? "upload-page--idle" : ""}`}>
      {toastMsg && <div className="upload-toast">{toastMsg}</div>}

      {/* Step 1 — upload area only */}
      {state === "idle" && (
        <>
          <header className="upload-page-header">
            <h1 className="upload-page-title">Upload a file</h1>
            <p className="upload-page-subtitle">
              Share securely with a link. Set expiry and download limits after selecting a file.
            </p>
          </header>

          <div className="upload-idle-body">
            {error && <div className="upload-alert upload-alert--error">{error}</div>}
            <Dropzone onFileSelected={handleFileSelected} />
            <p className="upload-constraint">Maximum file size: 10 MB</p>
          </div>
        </>
      )}

      {/* Step 2 — file card + settings + primary action */}
      {state === "selected" && selectedFile && (
        <div className="upload-panel">
          {error && <div className="upload-alert upload-alert--error">{error}</div>}

          <div className="upload-file-card">
            <div className="upload-file-thumb">
              {previewUrl ? (
                <img src={previewUrl} alt="" className="upload-file-thumb-image" />
              ) : (
                <FileIcon />
              )}
            </div>
            <div className="upload-file-meta">
              <p className="upload-file-name">{selectedFile.name}</p>
              <p className="upload-file-size">{formatBytes(selectedFile.size)}</p>
            </div>
            <span className="upload-file-ext">{getFileExtension(selectedFile)}</span>
          </div>

          <section className="upload-settings" aria-labelledby="upload-settings-heading">
            <h2 id="upload-settings-heading" className="upload-settings-heading">
              Share settings
            </h2>
            <p className="upload-settings-desc">
              Configure how long the link stays active and how many times it can be downloaded.
            </p>

            <div className="upload-field">
              <label htmlFor="expiry-value" className="upload-field-label">
                Link expiry
              </label>
              <div className="upload-field-group">
                <input
                  id="expiry-value"
                  type="number"
                  min="1"
                  value={expiryValue}
                  onChange={(e) => setExpiryValue(e.target.value)}
                  className="upload-field-input"
                  placeholder="7"
                />
                <select
                id="expiry-unit"
                value={expiryUnit}
                onChange={(e) => setExpiryUnit(e.target.value as "days" | "hours" | "minutes")}
                className="upload-field-select"
              >
                <option value="days">Days</option>
                <option value="hours">Hours</option>
                <option value="minutes">Minutes</option>
              </select>
              </div>
              <p className="upload-field-hint">Time before the link expires</p>
            </div>

            <div className="upload-field">
              <label htmlFor="download-limit" className="upload-field-label">
                Download limit
              </label>
              <input
                id="download-limit"
                type="number"
                min="1"
                value={downloadLimit}
                onChange={(e) => setDownloadLimit(e.target.value)}
                className="upload-field-input"
                placeholder="Unlimited"
              />
              <p className="upload-field-hint">Maximum number of times the file can be downloaded</p>
            </div>
          </section>

          <div className="upload-actions">
            <button onClick={handleShare} className="upload-btn-primary">
              Generate Share Link
            </button>
            <button onClick={handleReset} className="upload-btn-ghost">
              Choose a different file
            </button>
          </div>
        </div>
      )}

      {/* Uploading */}
      {state === "uploading" && selectedFile && (
        <div className="upload-progress-panel">
          <div className="upload-file-card">
            <div className="upload-file-thumb">
              {previewUrl ? (
                <img src={previewUrl} alt="" className="upload-file-thumb-image" />
              ) : (
                <FileIcon />
              )}
            </div>
            <div className="upload-file-meta">
              <p className="upload-file-name">{selectedFile.name}</p>
              <p className="upload-file-size">{formatBytes(selectedFile.size)}</p>
            </div>
            <span className="upload-file-ext">{getFileExtension(selectedFile)}</span>
          </div>

          <div className="upload-progress-status">
            <div className="upload-spinner" aria-hidden="true" />
            <span>Generating share link…</span>
          </div>

          <div className="upload-progress-details">
            <p className="upload-progress-detail">
              Expiry: {expiryValue ? `${expiryValue} ${expiryUnit}` : "None"}
            </p>
            <p className="upload-progress-detail">
              Download limit: {downloadLimit ? downloadLimit : "Unlimited"}
            </p>
          </div>
        </div>
      )}

      {/* Step 4 — success */}
      {state === "success" && (
        <div className="upload-success-panel">
          <div className="upload-success-banner" role="status">
            <span className="upload-success-icon" aria-hidden="true">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <polyline points="20 6 9 17 4 12" />
              </svg>
            </span>
            <div>
              <p className="upload-success-text">Share link ready</p>
              <p className="upload-success-subtext">Your file is uploaded and ready to share.</p>
            </div>
          </div>

          <div className="upload-link-section">
            <label htmlFor="share-link" className="upload-link-label">
              Share link
            </label>
            <div className="upload-link-row">
              <input id="share-link" readOnly value={shareLink} className="upload-link-input" />
              <button onClick={handleCopyLink} className="upload-btn-copy">
                Copy link
              </button>
            </div>
          </div>

          <button onClick={handleReset} className="upload-btn-primary">
            Share another file
          </button>
        </div>
      )}
    </div>
  );
};

export default UploadPage;
