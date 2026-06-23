import * as React from "react";

interface FilePreviewProps {
  mimeType: string;
  fileName: string;
  previewUrl: string | null;
}

const isImage = (mime: string): boolean => {
  const imgTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];
  return imgTypes.includes(mime.toLowerCase()) || mime.toLowerCase().startsWith("image/");
};

const getFileBadgeClass = (name: string): string => {
  const ext = name.split(".").pop()?.toLowerCase() || "";
  if (ext === "pdf") return "pdf";
  if (ext === "svg") return "svg";
  if (ext === "eps") return "eps";
  if (["png", "jpg", "jpeg", "gif", "webp"].includes(ext)) return "img";
  if (["zip", "rar", "tar", "gz", "7z"].includes(ext)) return "zip";
  if (["doc", "docx", "xls", "xlsx", "txt"].includes(ext)) return "doc";
  return "oth";
};

const getFileBadgeLabel = (name: string): string => {
  const ext = name.split(".").pop()?.toLowerCase() || "file";
  return ext.slice(0, 3).toUpperCase();
};

export const FilePreview: React.FC<FilePreviewProps> = ({
  mimeType,
  fileName,
  previewUrl,
}) => {
  const [imgError, setImgError] = React.useState(false);
  const fileIsImage = isImage(mimeType) && !imgError;

  return (
    <div className="preview-display" style={{ borderRadius: "12px", background: "#f8fafc", padding: "24px", display: "flex", justifyContent: "center", alignItems: "center", minHeight: "220px", marginBottom: "24px" }}>
      {fileIsImage && previewUrl ? (
        <img 
          src={previewUrl} 
          alt={fileName} 
          className="preview-image"
          onError={() => setImgError(true)}
          style={{ maxWidth: "100%", maxHeight: "350px", objectFit: "contain", borderRadius: "8px", boxShadow: "0 4px 12px rgba(0,0,0,0.05)" }}
        />
      ) : (
        <div className="preview-icon-wrapper" style={{ display: "flex", flexDirection: "column", alignItems: "center" }}>
          <div 
            className={`file-badge ${getFileBadgeClass(fileName)}`}
            style={{ width: "56px", height: "68px", fontSize: "12px", marginBottom: "16px", display: "flex", alignItems: "center", justifyContent: "center" }}
          >
            {getFileBadgeLabel(fileName)}
          </div>
          <p style={{ fontSize: "14px", color: "var(--text)", fontWeight: 500 }}>
            No inline preview available for this file type.
          </p>
        </div>
      )}
    </div>
  );
};
