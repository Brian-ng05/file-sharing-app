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

const ReviewPage: React.FC = () => {
  const { code } = useParams<{ code: string }>();
  const navigate = useNavigate();

  const [loading, setLoading] = React.useState(true);
  const [metadata, setMetadata] = React.useState<FileMetadata | null>(null);
  const [previewUrl, setPreviewUrl] = React.useState<string | null>(null);
  const [error, setError] = React.useState<string | null>(null);
  const [toastMsg, setToastMsg] = React.useState<string | null>(null);

  // E2EE password state
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
          // If not encrypted, resolve previewUrl from the cached blob immediately
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

  // Clean up Object URL to prevent memory leaks
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
      // Decrypt the cached blob in place
      const decryptedBlob = await fileService.decryptCachedBlob(code, password);
      
      // Decryption succeeded
      setNeedsDecryption(false);
      setPreviewUrl(URL.createObjectURL(decryptedBlob));
      showToast("Decrypted successfully!");
    } catch (err: any) {
      setModalError(err?.message || "Incorrect password or corrupted file.");
    }
  };

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

  if (loading) {
    return <Loading message="Retrieving file metadata..." />;
  }

  if (error || !metadata) {
    return <ErrorMessage message={error || "The file you are trying to view is no longer available."} />;
  }

  // If file is encrypted and needs decryption password, show the modal
  if (needsDecryption) {
    return (
      <PasswordModal 
        onSubmit={handlePasswordSubmit} 
        errorMessage={modalError} 
      />
    );
  }

  return (
    <div className="preview-container animate-fade-in">
      {toastMsg && <div className="toast">{toastMsg}</div>}

      <div className="preview-card" style={{ borderRadius: "20px", padding: "32px", boxShadow: "var(--shadow)" }}>
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
        <FilePreview 
          mimeType={metadata.mimeType} 
          fileName={metadata.originalFileName} 
          previewUrl={previewUrl} 
        />

        {/* Metadata Details List */}
        <FileInfo metadata={metadata} />

        {/* Action Controls */}
        <DownloadButton 
          onDownload={handleDownload} 
          onDelete={handleDelete} 
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

export default ReviewPage;
