import * as React from "react";
import { useParams, useNavigate } from "react-router-dom";
import { fileService } from "../services/file.service";
import { FileMetadata } from "../types/file";
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

  const [needsDecryption, setNeedsDecryption] = React.useState(false);
  const [modalError, setModalError] = React.useState<string | null>(null);

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

        if (meta.isEncrypted) {
          setNeedsDecryption(true);
        } else {
          const blob = fileService.getCachedBlob(code);
          if (blob) {
            setPreviewUrl(URL.createObjectURL(blob));
          }
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
      const decryptedBlob = await fileService.decryptCachedBlob(code, password);
      setNeedsDecryption(false);
      setPreviewUrl(URL.createObjectURL(decryptedBlob));
      showToast("Decrypted successfully");
    } catch (err: any) {
      setModalError(err?.message || "Incorrect password or corrupted file.");
    }
  };

  const handleDownload = async () => {
    if (!metadata || !code) return;

    try {
      await fileService.downloadFile(code, metadata.originalFileName);
      showToast("Download started");

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

  if (loading) {
    return <Loading message="Retrieving file metadata..." />;
  }

  if (error || !metadata) {
    return <ErrorMessage message={error || "The file you are trying to view is no longer available."} />;
  }

  if (needsDecryption) {
    return (
      <PasswordModal
        onSubmit={handlePasswordSubmit}
        errorMessage={modalError}
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
          <DownloadButton onDownload={handleDownload} onDelete={handleDelete} />
        </div>
      </div>
    </div>
  );
};

export default ReviewPage;
