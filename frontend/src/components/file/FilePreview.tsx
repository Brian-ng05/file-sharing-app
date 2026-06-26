import * as React from "react";
import "./FilePreview.css";

interface FilePreviewProps {
  mimeType: string;
  fileName: string;
  previewUrl: string | null;
}

const isImage = (mime: string): boolean => {
  const imgTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];
  return imgTypes.includes(mime.toLowerCase()) || mime.toLowerCase().startsWith("image/");
};

const getExtension = (name: string): string => {
  const ext = name.split(".").pop()?.toUpperCase() || "FILE";
  return ext.length > 6 ? "FILE" : ext;
};

export const FilePreview: React.FC<FilePreviewProps> = ({
  mimeType,
  fileName,
  previewUrl,
}) => {
  const [imgError, setImgError] = React.useState(false);
  const fileIsImage = isImage(mimeType) && !imgError;

  return (
    <div className="portal-preview">
      {fileIsImage && previewUrl ? (
        <img
          src={previewUrl}
          alt={fileName}
          className="portal-preview-image"
          onError={() => setImgError(true)}
        />
      ) : (
        <div className="portal-preview-placeholder">
          <svg
            className="portal-preview-icon"
            width="48"
            height="48"
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
          <span className="portal-preview-ext">{getExtension(fileName)}</span>
          <p className="portal-preview-message">
            Preview is not available for this file type. Download to view the contents.
          </p>
        </div>
      )}
    </div>
  );
};
