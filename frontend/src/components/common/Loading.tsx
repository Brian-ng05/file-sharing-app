import * as React from "react";

interface LoadingProps {
  message?: string;
}

export const Loading: React.FC<LoadingProps> = ({ message = "Retrieving file metadata..." }) => {
  return (
    <div 
      className="card animate-fade-in" 
      style={{ 
        display: "flex", 
        flexDirection: "column", 
        alignItems: "center", 
        justifyContent: "center", 
        minHeight: "300px", 
        borderRadius: "20px",
        padding: "48px 32px",
        boxShadow: "var(--shadow)"
      }}
    >
      <div className="spinner-wrapper" style={{ position: "relative", width: "80px", height: "80px", marginBottom: "24px" }}>
        <div className="spinner-outer" />
        <div className="spinner-inner" />
        <div className="spinner-center" style={{ position: "absolute", top: "50%", left: "50%", transform: "translate(-50%, -50%)", fontSize: "28px" }}>
          🔄
        </div>
      </div>
      <p style={{ color: "var(--text)", fontWeight: 500, margin: 0, fontSize: "16px" }}>{message}</p>
      
      <style>{`
        .spinner-outer {
          box-sizing: border-box;
          width: 80px;
          height: 80px;
          border: 4px solid var(--accent-bg);
          border-top: 4px solid var(--accent);
          border-radius: 50%;
          animation: spin 1.2s cubic-bezier(0.5, 0, 0.5, 1) infinite;
        }
        .spinner-inner {
          box-sizing: border-box;
          position: absolute;
          top: 8px;
          left: 8px;
          width: 64px;
          height: 64px;
          border: 4px solid transparent;
          border-bottom: 4px solid #3b82f6;
          border-radius: 50%;
          animation: spin-reverse 1.8s linear infinite;
        }
        @keyframes spin {
          0% { transform: rotate(0deg); }
          100% { transform: rotate(360deg); }
        }
        @keyframes spin-reverse {
          0% { transform: rotate(360deg); }
          100% { transform: rotate(0deg); }
        }
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
