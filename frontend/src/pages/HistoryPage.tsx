import * as React from "react";
import { Link } from "react-router-dom";
import { historyService } from "../services/history.service";
import { fileService } from "../services/file.service";
import { FileMetadata } from "../types/file";
import "./HistoryPage.css";

const FileIcon: React.FC = () => (
  <svg
    width="16"
    height="16"
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

const HistoryPage: React.FC = () => {
  const [historyList, setHistoryList] = React.useState<FileMetadata[]>([]);
  const [toastMsg, setToastMsg] = React.useState<string | null>(null);
  const [currentPage, setCurrentPage] = React.useState<number>(1);
  const itemsPerPage = 6;

  React.useEffect(() => {
    setHistoryList(historyService.getHistory());
  }, []);

  const showToast = (msg: string) => {
    setToastMsg(msg);
    setTimeout(() => setToastMsg(null), 3000);
  };

  const copyLink = (code: string) => {
    const link = `${window.location.origin}/f/${code}`;
    navigator.clipboard.writeText(link).then(() => {
      showToast("Link copied to clipboard!");
    });
  };

  const handleDelete = async (code: string) => {
    if (!confirm("Are you sure you want to delete this file from storage?")) {
      return;
    }

    try {
      await fileService.deleteFile(code);
      const updatedHistory = historyService.getHistory();
      setHistoryList(updatedHistory);
      
      // Update current page if needed
      const newTotalPages = Math.ceil(updatedHistory.length / itemsPerPage);
      if (currentPage > newTotalPages && newTotalPages > 0) {
        setCurrentPage(newTotalPages);
      }
      
      showToast("File deleted successfully.");
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

  const formatExpiry = (expiresAt?: string): string => {
    if (!expiresAt) return "Active";
    const diff = new Date(expiresAt).getTime() - Date.now();
    if (diff <= 0) return "Expired";

    const hours = Math.floor(diff / (1000 * 60 * 60));
    if (hours > 24) {
      return `${Math.floor(hours / 24)} days left`;
    }
    if (hours > 0) {
      return `${hours} hrs left`;
    }
    const mins = Math.floor(diff / (1000 * 60));
    return `${mins} mins left`;
  };

  const isExpired = (expiresAt?: string): boolean => {
    if (!expiresAt) return false;
    return new Date(expiresAt).getTime() <= Date.now();
  };

  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / (1000 * 60));
    const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

    if (diffMins < 1) return "just now";
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays === 1) return "yesterday";
    if (diffDays < 7) return `${diffDays} days ago`;
    return date.toLocaleDateString();
  };

  const formatDownloads = (file: FileMetadata): string => {
    if (file.maxDownloads) {
      return `${file.downloadCount}/${file.maxDownloads}`;
    }
    return file.downloadCount > 0 ? String(file.downloadCount) : "—";
  };

  // Pagination logic
  const totalPages = Math.ceil(historyList.length / itemsPerPage);
  const indexOfLastItem = currentPage * itemsPerPage;
  const indexOfFirstItem = indexOfLastItem - itemsPerPage;
  const currentItems = historyList.slice(indexOfFirstItem, indexOfLastItem);

  const formatMetaLine = (file: FileMetadata): React.ReactNode => {
    const parts: React.ReactNode[] = [
      formatBytes(file.sizeBytes),
      <>Uploaded {formatDate(file.createdAt)}</>,
    ];

    if (file.maxDownloads) {
      parts.push(`${file.downloadCount}/${file.maxDownloads} downloads`);
    }

    const expired = isExpired(file.expiresAt);
    const expiryText = formatExpiry(file.expiresAt);
    parts.push(
      expired ? (
        <span className="history-row-status--expired">{expiryText}</span>
      ) : expiryText === "Active" ? (
        <span className="history-row-status--active">{expiryText}</span>
      ) : (
        expiryText
      )
    );

    return parts.map((part, i) => (
      <React.Fragment key={i}>
        {i > 0 && <span className="history-row-meta-sep">•</span>}
        {part}
      </React.Fragment>
    ));
  };

  return (
    <div className="history-page">
      {toastMsg && <div className="history-toast">{toastMsg}</div>}

      <header className="history-header">
        <h1 className="history-title">Upload History</h1>
        <p className="history-subtitle">View and manage files you have shared</p>
      </header>

      {historyList.length === 0 ? (
        <div className="history-empty">
          <div className="history-empty-icon" aria-hidden="true">
            <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
              <polyline points="14 2 14 8 20 8" />
            </svg>
          </div>
          <h2 className="history-empty-title">No uploads found</h2>
          <p className="history-empty-desc">
            You haven&apos;t uploaded any files yet, or they have all expired.
          </p>
          <Link to="/" className="history-empty-link">
            Upload a file
          </Link>
        </div>
      ) : (
        <>
          <ul className="history-list" aria-label="Uploaded files">
            <li className="history-list-head" aria-hidden="true">
              <span />
              <span>Name</span>
              <span>Size</span>
              <span>Uploaded</span>
              <span>Expiry</span>
              <span>Downloads</span>
              <span />
            </li>

            {currentItems.map((file) => {
              const expired = isExpired(file.expiresAt);
              const expiryText = formatExpiry(file.expiresAt);

              return (
                <li key={file.code} className="history-row">
                  <div className="history-row-icon">
                    <FileIcon />
                  </div>

                  <div className="history-row-main">
                    <span className="history-row-name" title={file.originalFileName}>
                      {file.originalFileName}
                    </span>
                  </div>

                  <span className="history-row-meta">{formatMetaLine(file)}</span>

                  <span className="history-row-size">{formatBytes(file.sizeBytes)}</span>
                  <span className="history-row-date">{formatDate(file.createdAt)}</span>
                  <span
                    className={`history-row-expiry ${
                      expired
                        ? "history-row-status--expired"
                        : expiryText === "Active"
                          ? "history-row-status--active"
                          : ""
                    }`}
                  >
                    {expiryText}
                  </span>
                  <span className="history-row-downloads">{formatDownloads(file)}</span>

                  <div className="history-row-actions">
                    <Link to={`/f/${file.code}`} className="history-action history-action--primary">
                      View
                    </Link>
                    <button
                      type="button"
                      className="history-action"
                      onClick={() => copyLink(file.code)}
                      title="Copy shareable link"
                    >
                      Copy Link
                    </button>
                    <button
                      type="button"
                      className="history-action history-action--danger"
                      onClick={() => handleDelete(file.code)}
                      title="Delete file"
                    >
                      Delete
                    </button>
                  </div>
                </li>
              );
            })}
          </ul>

          {totalPages > 1 && (
            <div className="history-pagination">
              <button
                className="history-pagination-btn"
                onClick={() => setCurrentPage(currentPage - 1)}
                disabled={currentPage === 1}
              >
                Previous
              </button>
              <span className="history-pagination-info">
                Page {currentPage} of {totalPages}
              </span>
              <button
                className="history-pagination-btn"
                onClick={() => setCurrentPage(currentPage + 1)}
                disabled={currentPage === totalPages}
              >
                Next
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
};

export default HistoryPage;
