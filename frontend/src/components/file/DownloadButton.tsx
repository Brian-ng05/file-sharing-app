import * as React from "react";
import { Link } from "react-router-dom";
import "./DownloadButton.css";

interface DownloadButtonProps {
  onDownload: () => void;
  onDelete: () => void;
  isDownloading?: boolean;
  showDelete?: boolean;
  disabled?: boolean;
}

export const DownloadButton: React.FC<DownloadButtonProps> = ({
  onDownload,
  onDelete,
  isDownloading = false,
  showDelete = true,
  disabled = false,
}) => {
  const btnDisabled = disabled || isDownloading;

  return (
    <div className="portal-actions">
      <button
        type="button"
        className={`portal-download ${btnDisabled ? "portal-download--disabled" : ""}`}
        onClick={onDownload}
        disabled={btnDisabled}
      >
        <svg
          className="portal-download-icon"
          width="18"
          height="18"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
          aria-hidden="true"
        >
          <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
          <polyline points="7 10 12 15 17 10" />
          <line x1="12" y1="15" x2="12" y2="3" />
        </svg>
        {isDownloading ? "Downloading…" : "Download file"}
      </button>

      <div className="portal-secondary-actions">
        {showDelete && (
          <button
            type="button"
            className="portal-secondary-btn portal-secondary-btn--danger"
            onClick={onDelete}
          >
            Delete
          </button>
        )}
        <Link to="/" className="portal-secondary-btn">
          Upload another
        </Link>
      </div>
    </div>
  );
};
