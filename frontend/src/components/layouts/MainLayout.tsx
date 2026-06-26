import * as React from "react";
import { Outlet } from "react-router-dom";
import Navbar from "../common/Navbar";
// @ts-ignore: CSS import type declarations are not available in this file
import "./MainLayout.css";

const MainLayout: React.FC = () => {
  return (
    <div className="layout-container">
      <Navbar />

      <div className="layout-main">
        <main className="layout-content">
          <Outlet />
        </main>
        <footer className="layout-footer">
          <p>&copy; {new Date().getFullYear()} DropShare. Premium File Sharing Service.</p>
        </footer>
      </div>
    </div>
  );
};

export default MainLayout;
