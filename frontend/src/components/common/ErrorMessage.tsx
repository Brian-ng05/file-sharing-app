import * as React from "react";
import { Link } from "react-router-dom";

interface ErrorMessageProps {
  title?: string;
  message: string;
  showUploadButton?: boolean;
  showHistoryButton?: boolean;
}

/**
 * Picks a contextual title based on keywords in the error message.
 */
function inferTitle(message: string): string {
  const lower = message.toLowerCase();
  if (lower.includes("expir") || lower.includes("unavailable")) {
    return "File Expired / Unavailable";
  }
  if (lower.includes("not found") || lower.includes("deleted")) {
    return "File Not Found";
  }
  if (lower.includes("password") || lower.includes("unauthorized")) {
    return "Password Required";
  }
  if (lower.includes("limit") || lower.includes("download")) {
    return "Download Limit Reached";
  }
  if (lower.includes("storage") || lower.includes("unavailable")) {
    return "Service Unavailable";
  }
  if (lower.includes("network")) {
    return "Network Error";
  }
  return "Access Denied / Expired";
}

export const ErrorMessage: React.FC<ErrorMessageProps> = ({
  title,
  message,
  showUploadButton = true,
  showHistoryButton = true,
}) => {
  const displayTitle = title || inferTitle(message);

  return (
    <div
      className="card animate-fade-in"
      style={{
        textAlign: "center",
        padding: "48px 32px",
        borderRadius: "20px",
        boxShadow: "var(--shadow)",
        maxWidth: "600px",
        margin: "0 auto",
      }}
    >
      <div style={{ marginBottom: "20px" }}>
        <svg width="56" height="56" viewBox="0 0 24 24" fill="none" stroke="#cf222e" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
          <circle cx="12" cy="12" r="10" />
          <line x1="12" y1="8" x2="12" y2="12" />
          <line x1="12" y1="16" x2="12.01" y2="16" />
        </svg>
      </div>
      <h2 style={{ color: "var(--text-h)", fontWeight: 700, margin: "0 0 12px" }}>{displayTitle}</h2>
      <p style={{ color: "var(--text)", marginBottom: "32px", fontSize: "15px", lineHeight: "1.6" }}>
        {message}
      </p>

      {(showUploadButton || showHistoryButton) && (
        <div className="action-row" style={{ display: "flex", justifyContent: "center", gap: "16px", flexWrap: "wrap" }}>
          {showUploadButton && (
            <Link
              to="/"
              className="btn btn-primary"
              style={{ background: "#0969da", borderRadius: "10px", padding: "10px 24px", textDecoration: "none", color: "#fff", fontWeight: 600, fontSize: "14px" }}
            >
              Upload a File
            </Link>
          )}
          {showHistoryButton && (
            <Link
              to="/history"
              className="btn btn-secondary"
              style={{ borderRadius: "10px", padding: "10px 24px", textDecoration: "none", color: "#1f2328", fontWeight: 500, fontSize: "14px", border: "1px solid #d0d7de" }}
            >
              Go to History
            </Link>
          )}
        </div>
      )}
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
