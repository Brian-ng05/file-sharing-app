import * as React from "react";
import { NavLink } from "react-router-dom";
import { isMockMode, setMockMode } from "../../services/api";
import "./Navbar.css";

const Navbar: React.FC = () => {
  const [mockActive, setMockActive] = React.useState(isMockMode());
  const [menuOpen, setMenuOpen] = React.useState(false);

  const handleToggleMock = () => {
    const newValue = !mockActive;
    setMockMode(newValue);
    setMockActive(newValue);
    window.location.reload();
  };

  const closeMenu = () => setMenuOpen(false);

  return (
    <header className="app-header">
      <div className="app-header-inner">
        <NavLink to="/" className="app-header-brand" onClick={closeMenu}>
          <div className="app-header-brand-icon" aria-hidden="true" />
          <span className="app-header-brand-text">DropShare</span>
        </NavLink>

        <nav className="app-header-nav" aria-label="Main navigation">
          <NavLink
            to="/"
            end
            className={({ isActive }) => `app-header-link ${isActive ? "active" : ""}`}
          >
            Upload
          </NavLink>
          <NavLink
            to="/history"
            className={({ isActive }) => `app-header-link ${isActive ? "active" : ""}`}
          >
            History
          </NavLink>
        </nav>

        <div className="app-header-actions">
          <button
            type="button"
            className="app-header-mock"
            onClick={handleToggleMock}
            title="Toggle between Mock and Real API modes"
          >
            {mockActive ? "Mock Mode" : "Real API"}
          </button>

          <button
            type="button"
            className="app-header-menu-btn"
            onClick={() => setMenuOpen(!menuOpen)}
            aria-label="Toggle menu"
            aria-expanded={menuOpen}
          >
            <span className={`app-header-menu-line ${menuOpen ? "open" : ""}`} />
            <span className={`app-header-menu-line ${menuOpen ? "open" : ""}`} />
            <span className={`app-header-menu-line ${menuOpen ? "open" : ""}`} />
          </button>
        </div>
      </div>

      {menuOpen && (
        <div className="app-header-overlay" onClick={closeMenu}>
          <nav
            className="app-header-drawer"
            aria-label="Mobile navigation"
            onClick={(e) => e.stopPropagation()}
          >
            <NavLink
              to="/"
              end
              className={({ isActive }) => `app-header-drawer-link ${isActive ? "active" : ""}`}
              onClick={closeMenu}
            >
              Upload
            </NavLink>
            <NavLink
              to="/history"
              className={({ isActive }) => `app-header-drawer-link ${isActive ? "active" : ""}`}
              onClick={closeMenu}
            >
              History
            </NavLink>
            <div className="app-header-drawer-divider" />
            <button type="button" className="app-header-drawer-mock" onClick={handleToggleMock}>
              {mockActive ? "Mock Mode" : "Real API"}
            </button>
          </nav>
        </div>
      )}
    </header>
  );
};

export default Navbar;
