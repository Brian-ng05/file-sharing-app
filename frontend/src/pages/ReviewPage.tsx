import * as React from "react";
import { useParams, useNavigate } from "react-router-dom";
import { fileService } from "../services/file.service";
import { FileMetadata } from "../types/file";
import { isMockMode } from "../services/api";
import { Loading } from "../components/common/Loading";
import { ErrorMessage } from "../components/common/ErrorMessage";
import { FilePreview } from "../components/file/FilePreview";
import { FileInfo } from "../components/file/FileInfo";
import { DownloadButton } from "../components/file/DownloadButton";
import { PasswordModal } from "../components/file/PasswordModal";
import "./ReviewPage.css";

const ReviewPage: React.FC = () => {
  const { code } = useParams<{ code: string }>();
  const navigate = useNavigate();

  const [loading, setLoading] = React.useState(true);
  const [metadata, setMetadata] = React.useState<FileMetadata | null>(null);
  const [previewUrl, setPreviewUrl] = React.useState<string | null>(null);
  const [error, setError] = React.useState<string | null>(null);
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

  React.useEffect(() => {
    if (!code) return;

    const fetchFileData = async () => {
      setLoading(true);
      setError(null);
      try {
        const meta = await fileService.getFileMetadata(code);
        setMetadata(meta);

        // In mock mode, try to get cached blob for preview
        if (isMockMode()) {
          const blob = fileService.getCachedBlob(code);
          if (blob) {
            setPreviewUrl(URL.createObjectURL(blob));
          }
        }

        // If server says password required, show password modal
        if (meta.requiresPassword) {
          setNeedsPassword(true);
        }
      } catch (err: any) {
        setError(err?.message || "File not found or has expired.");
      } finally {
        setLoading(false);
      }
    };

    fetchFileData();
  }, [code]);

  React.useEffect(() => {
    return () => {
      if (previewUrl) {
        URL.revokeObjectURL(previewUrl);
      }
    };
  }, [previewUrl]);

  const handlePasswordSubmit = async (password: string) => {
    if (!code) return;
    setModalError(null);
    try {
      await fileService.verifyPassword(code, password);
      setNeedsPassword(false);
      setVerifiedPassword(password);
      showToast("Password verified successfully");
    } catch (err: any) {
      setModalError(err?.message || "Incorrect password.");
    }
  };

  const handlePasswordCancel = () => {
    navigate("/");
  };

  const handleDownload = async () => {
    if (!metadata || !code || isDownloading) return;
    setIsDownloading(true);

    try {
      await fileService.downloadFile(code, metadata.originalFileName, verifiedPassword || undefined);
      showToast("Download started");

      setMetadata((prev) => {
        if (!prev) return null;
        const newCount = (prev.downloadCount ?? 0) + 1;

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

  if (error || !metadata) {
    return (
      <ErrorMessage
        message={error || "The file you are trying to view is no longer available."}
        showUploadButton
        showHistoryButton
      />
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

  return (
    <div className="portal-page">
      {toastMsg && <div className="portal-toast">{toastMsg}</div>}

      <div className="portal-layout">
        <FilePreview
          mimeType={metadata.mimeType}
          fileName={metadata.originalFileName}
          previewUrl={previewUrl}
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
          />
        </div>
      </div>
    </div>
  );
};

export default ReviewPage;
