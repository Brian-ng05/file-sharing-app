import * as React from "react";
import { Link } from "react-router-dom";

interface ErrorMessageProps {
  title?: string;
  message: string;
  showUploadButton?: boolean;
  showHistoryButton?: boolean;
}

export const ErrorMessage: React.FC<ErrorMessageProps> = ({
  title = "Access Denied / Expired",
  message,
  showUploadButton = true,
  showHistoryButton = true,
}) => {
  return (
    <div 
      className="card animate-fade-in" 
      style={{ 
        textAlign: "center", 
        padding: "48px 32px", 
        borderRadius: "20px",
        boxShadow: "var(--shadow)",
        maxWidth: "600px",
        margin: "0 auto"
      }}
    >
      <div style={{ fontSize: "56px", marginBottom: "20px" }}>⚠️</div>
      <h2 style={{ color: "var(--text-h)", fontWeight: 700, margin: "0 0 12px" }}>{title}</h2>
      <p style={{ color: "var(--text)", marginBottom: "32px", fontSize: "15px", lineHeight: "1.6" }}>
        {message}
      </p>
      
      {(showUploadButton || showHistoryButton) && (
        <div className="action-row" style={{ display: "flex", justifyContent: "center", gap: "16px" }}>
          {showUploadButton && (
            <Link 
              to="/" 
              className="btn btn-primary"
              style={{ background: "#1e0094", borderRadius: "10px", padding: "10px 24px", textDecoration: "none" }}
            >
              Upload a File
            </Link>
          )}
          {showHistoryButton && (
            <Link 
              to="/history" 
              className="btn btn-secondary" 
              style={{ borderRadius: "10px", padding: "10px 24px", textDecoration: "none" }}
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
