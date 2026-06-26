import * as React from "react";
import "./PasswordModal.css";
import "./DownloadButton.css";

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
    <div className="portal-page">
      <div className="portal-unlock">
        <div className="portal-unlock-icon" aria-hidden="true">
          <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
            <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
            <path d="M7 11V7a5 5 0 0 1 10 0v4" />
          </svg>
        </div>
        <h2 className="portal-unlock-title">Password required</h2>
        <p className="portal-unlock-desc">
          This file is encrypted. Enter the password to preview and download.
        </p>

        <form onSubmit={handleSubmit} className="portal-unlock-form">
          <input
            type="password"
            className={`portal-unlock-input ${errorMessage ? "portal-unlock-input--error" : ""}`}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="Enter password"
            autoFocus
          />
          {errorMessage && <p className="portal-unlock-error">{errorMessage}</p>}

          <button type="submit" className="portal-download" disabled={!password.trim()}>
            Unlock file
          </button>
        </form>
      </div>
    </div>
  );
};
