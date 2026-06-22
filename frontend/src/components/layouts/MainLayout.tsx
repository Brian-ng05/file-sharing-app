import * as React from "react";
import { Outlet } from "react-router-dom";
import Navbar from "../common/Navbar";

const MainLayout: React.FC = () => {
  return (
    <>
      <Navbar />
      <main className="app-main">
        <Outlet />
      </main>
      <footer className="app-footer">
        <p>&copy; {new Date().getFullYear()} DropShare. Premium File Sharing Service.</p>
      </footer>
    </>
  );
};

export default MainLayout;