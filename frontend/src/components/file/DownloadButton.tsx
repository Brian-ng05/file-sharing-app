import * as React from "react";
import { Link } from "react-router-dom";

interface DownloadButtonProps {
  onDownload: () => void;
  onDelete: () => void;
  isDownloading?: boolean;
}

export const DownloadButton: React.FC<DownloadButtonProps> = ({
  onDownload,
  onDelete,
  isDownloading = false,
}) => {
  return (
    <div className="action-row" style={{ display: "flex", flexWrap: "wrap", gap: "12px", justifyContent: "center", marginTop: "24px" }}>
      <button 
        type="button" 
        className="btn btn-primary" 
        onClick={onDownload}
        disabled={isDownloading}
        style={{ minWidth: "150px", background: "#1e0094", borderRadius: "10px", padding: "12px 24px" }}
      >
        {isDownloading ? "Downloading..." : "Download File"}
      </button>
      
      <button 
        type="button" 
        className="btn btn-danger" 
        onClick={onDelete}
        style={{ borderRadius: "10px", padding: "12px 24px" }}
      >
        Delete File
      </button>

      <Link 
        to="/" 
        className="btn btn-secondary"
        style={{ borderRadius: "10px", padding: "12px 24px", textDecoration: "none", display: "inline-flex", alignItems: "center", justifyContent: "center" }}
      >
        Upload Another
      </Link>
    </div>
  );
};
