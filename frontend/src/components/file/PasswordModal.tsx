import * as React from "react";

interface PasswordModalProps {
  onSubmit: (password: string) => void;
  errorMessage: string | null;
}

export const PasswordModal: React.FC<PasswordModalProps> = ({ onSubmit, errorMessage }) => {
  const [password, setPassword] = React.useState("");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (password.trim()) {
      onSubmit(password);
    }
  };

  return (
    <div 
      className="card" 
      style={{ 
        maxWidth: "480px", 
        margin: "40px auto", 
        textAlign: "center", 
        borderRadius: "20px", 
        padding: "40px 32px",
        boxShadow: "var(--shadow)"
      }}
    >
      <div style={{ fontSize: "56px", marginBottom: "16px" }}>🔒</div>
      <h2 style={{ color: "#1e1b4b", fontWeight: 700, margin: "0 0 8px" }}>Password Protected</h2>
      <p style={{ color: "var(--text)", fontSize: "14px", marginBottom: "24px" }}>
        This file is end-to-end encrypted. Enter the decryption password to view and download it.
      </p>

      <form onSubmit={handleSubmit}>
        <div className="form-group" style={{ marginBottom: "20px" }}>
          <input
            type="password"
            className="form-input"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="Enter password"
            style={{ 
              padding: "12px", 
              fontSize: "15px", 
              textAlign: "center",
              borderRadius: "10px",
              border: errorMessage ? "1px solid #ef4444" : "1px solid var(--border)"
            }}
            autoFocus
          />
          {errorMessage && (
            <div style={{ color: "#ef4444", fontSize: "13px", marginTop: "8px", fontWeight: 500 }}>
              {errorMessage}
            </div>
          )}
        </div>

        <button
          type="submit"
          className="btn btn-primary"
          style={{ 
            width: "100%", 
            background: "#1e0094", 
            borderRadius: "10px", 
            padding: "12px" 
          }}
          disabled={!password.trim()}
        >
          Decrypt & Preview
        </button>
      </form>
    </div>
  );
};
