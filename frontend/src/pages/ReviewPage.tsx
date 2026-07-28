import * as React from "react";
import { useParams, useNavigate, Link } from "react-router-dom";
import { fileService, FileError } from "../services/file.service";
import { historyService } from "../services/history.service";
import { FileMetadata, FileErrorType } from "../types/file";
import { formatExpiryDisplay } from "../utils/expiry";
import { useNow } from "../utils/useNow";

import { Loading } from "../components/common/Loading";
import { FilePreview } from "../components/file/FilePreview";
import { FileInfo } from "../components/file/FileInfo";
import { DownloadButton } from "../components/file/DownloadButton";
import { PasswordModal } from "../components/file/PasswordModal";
import "./ReviewPage.css";

interface ErrorState {
  type: FileErrorType;
  title: string;
  description: string;
}

function buildErrorState(type: FileErrorType, description: string): ErrorState {
  switch (type) {
    case "not_found":
      return { type, title: "File Not Found", description };
    case "expired":
      return { type, title: "File Expired", description };
    case "download_limit":
      return { type, title: "Download Limit Reached", description };
    case "server_error":
      return { type, title: "Service Temporarily Unavailable", description };
    case "network_error":
      return { type, title: "Cannot Connect to Server", description };
    case "password_required":
      return { type, title: "Password Required", description };
    case "invalid_password":
      return { type, title: "Incorrect Password", description };
  }
}

const ReviewPage: React.FC = () => {
  const { code } = useParams<{ code: string }>();
  const navigate = useNavigate();
  const now = useNow();

  const [loading, setLoading] = React.useState(true);
  const [metadata, setMetadata] = React.useState<FileMetadata | null>(null);
  const [errorState, setErrorState] = React.useState<ErrorState | null>(null);
  const [toastMsg, setToastMsg] = React.useState<string | null>(null);

  const [needsPassword, setNeedsPassword] = React.useState(false);
  const [verifiedPassword, setVerifiedPassword] = React.useState<string | null>(null);
  const [modalError, setModalError] = React.useState<string | null>(null);
  const [isDownloading, setIsDownloading] = React.useState(false);

  const showToast = (msg: string) => {
    setToastMsg(msg);
    setTimeout(() => setToastMsg(null), 3000);
  };

  const shareLink = React.useMemo(() => {
    if (!code) return "";
    return `${window.location.origin}/f/${code}`;
  }, [code]);

  const handleCopyLink = () => {
    navigator.clipboard.writeText(shareLink).then(() => {
      showToast("Link copied to clipboard");
    });
  };

  const fetchFileData = React.useCallback(async () => {
    if (!code) return;
    setLoading(true);
    setErrorState(null);
    try {
      const meta = await fileService.getFileMetadata(code);

      // Merge download count / max from local history if backend doesn't provide them
      const historyList = historyService.getHistory();
      const historyItem = historyList.find((h) => h.code === code);
      if (historyItem) {
        if (meta.maxDownloads === undefined && historyItem.maxDownloads !== undefined) {
          meta.maxDownloads = historyItem.maxDownloads;
        }
        if (meta.downloadCount === undefined && historyItem.downloadCount !== undefined) {
          meta.downloadCount = historyItem.downloadCount;
        }
      }

      setMetadata(meta);

      // If server says password required, show password modal
      if (meta.requiresPassword) {
        setNeedsPassword(true);
      }
    } catch (err: any) {
      if (err instanceof FileError) {
        setErrorState(buildErrorState(err.type, err.message));
      } else {
        setErrorState(buildErrorState("server_error", err?.message || "An unexpected error occurred."));
      }
    } finally {
      setLoading(false);
    }
  }, [code]);

  React.useEffect(() => {
    fetchFileData();
  }, [fetchFileData]);

  const handlePasswordSubmit = async (password: string) => {
    if (!code) return;
    setModalError(null);
    try {
      await fileService.verifyPassword(code, password);
      setNeedsPassword(false);
      setVerifiedPassword(password);
      showToast("Password verified successfully");
    } catch (err: any) {
      if (err instanceof FileError && err.type === "network_error") {
        setErrorState(buildErrorState("network_error", err.message));
      } else {
        setModalError(err?.message || "Incorrect password. Please try again.");
      }
    }
  };

  const handlePasswordCancel = () => {
    navigate("/");
  };

  // Compute disabled state for Download button
  const hasDownloadLimit = metadata?.maxDownloads !== undefined && metadata.maxDownloads > 0;
  const currentDownloads = metadata?.downloadCount ?? 0;
  const limitReached = hasDownloadLimit && currentDownloads >= metadata.maxDownloads!;
  const isExpired = metadata?.expiresAt ? formatExpiryDisplay(metadata.expiresAt, now).expired : false;
  const downloadDisabled = isExpired || limitReached || isDownloading;

  const handleDownload = async () => {
    if (!metadata || !code || downloadDisabled) return;

    setIsDownloading(true);

    try {
      await fileService.downloadFile(code, metadata.originalFileName, verifiedPassword || undefined);
      showToast("Download started");

      // Update local download count in history (best-effort)
      historyService.updateDownloadCount(code);

      setMetadata((prev) => {
        if (!prev) return null;
        const newCount = Math.min((prev.downloadCount ?? 0) + 1, prev.maxDownloads ?? Infinity);
        return {
          ...prev,
          downloadCount: newCount,
        };
      });
    } catch (err: any) {
      if (err instanceof FileError) {
        if (err.type === "invalid_password") {
          // Re-show password modal with error
          setNeedsPassword(true);
          setModalError(err.message);
        } else {
          // Show error card for download_limit, expired, not_found, server_error, network_error
          setErrorState(buildErrorState(err.type, err.message));
          setMetadata(null);
        }
      } else {
        showToast(err?.message || "Download failed.");
      }
    } finally {
      setIsDownloading(false);
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

  if (loading) {
    return <Loading message="Checking file availability…" />;
  }

  // Error state — distinct cards per error type
  if (errorState) {
    const isRecoverable = errorState.type === "server_error" || errorState.type === "network_error";
    return (
      <div className="review-error">
        <div className={`review-error-card`}>
          <h2 className="review-error-title">{errorState.title}</h2>
          <p className="review-error-desc">{errorState.description}</p>
          <div className="review-error-actions">
            {isRecoverable && (
              <button type="button" className="review-error-btn review-error-btn--primary" onClick={fetchFileData}>
                Try Again
              </button>
            )}
            <Link to="/" className="review-error-btn review-error-btn--primary">
              Upload a File
            </Link>
            <Link to="/history" className="review-error-btn review-error-btn--outline">
              Go to History
            </Link>
          </div>
        </div>
      </div>
    );
  }

  if (needsPassword) {
    return (
      <PasswordModal
        onSubmit={handlePasswordSubmit}
        errorMessage={modalError}
        onCancel={handlePasswordCancel}
      />
    );
  }

  if (!metadata) {
    return (
      <div className="review-error">
        <div className="review-error-card">
          <h2 className="review-error-title">File Not Found</h2>
          <p className="review-error-desc">The file you are looking for is no longer available.</p>
          <div className="review-error-actions">
            <Link to="/" className="review-error-btn review-error-btn--primary">Upload a File</Link>
            <Link to="/history" className="review-error-btn review-error-btn--outline">Go to History</Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="portal-page">
      {toastMsg && <div className="portal-toast">{toastMsg}</div>}

      <div className="portal-layout">
        <FilePreview
          mimeType={metadata.mimeType}
          fileName={metadata.originalFileName}
          previewUrl={null}
        />

        <div className="portal-details">
          <h1 className="portal-filename">{metadata.originalFileName}</h1>
          <FileInfo metadata={metadata} />

          <div className="portal-share-section">
            <label className="portal-share-label">Share link</label>
            <div className="portal-share-row">
              <input
                className="portal-share-input"
                type="text"
                readOnly
                value={shareLink}
                onClick={(e) => (e.target as HTMLInputElement).select()}
              />
              <button type="button" className="portal-btn-copy" onClick={handleCopyLink}>
                Copy
              </button>
            </div>
          </div>

          <DownloadButton
            onDownload={handleDownload}
            onDelete={handleDelete}
            isDownloading={isDownloading}
            showDelete={false}
            disabled={downloadDisabled}
          />
        </div>
      </div>
    </div>
  );
};

export default ReviewPage;
