import * as React from "react";
import { NavLink } from "react-router-dom";
import { isMockMode, setMockMode } from "../../services/api";

const Navbar: React.FC = () => {
  const [mockActive, setMockActive] = React.useState(isMockMode());

  const handleToggleMock = () => {
    const newValue = !mockActive;
    setMockMode(newValue);
    setMockActive(newValue);
    // Reload the page to reset services
    window.location.reload();
  };

  return (
    <header className="app-header">
      <nav className="navbar">
        <NavLink to="/" className="brand">
          <span className="brand-icon" />
          <span>DropShare</span>
        </NavLink>
        
        <div className="nav-links">
          <NavLink 
            to="/" 
            className={({ isActive }) => `nav-item ${isActive ? "active" : ""}`}
          >
            Upload
          </NavLink>
          
          <NavLink 
            to="/history" 
            className={({ isActive }) => `nav-item ${isActive ? "active" : ""}`}
          >
            History
          </NavLink>

          <button 
            type="button" 
            className="mock-badge" 
            onClick={handleToggleMock} 
            title="Click to toggle between Mock and Real API modes"
            style={{ cursor: "pointer", border: "1px solid var(--accent-border)" }}
          >
            {mockActive ? "Mock Mode" : "Real API"}
          </button>
        </div>
      </nav>
    </header>
  );
};

export default Navbar;