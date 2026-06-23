# DropShare Frontend - Web Client (Phases 1 & 2)

This directory houses the React + TypeScript frontend application for the File & Image Sharing Service, representing the complete implementation of **Giai đoạn 1 (Core MVP)** and **Giai đoạn 2 (Real-time Upload Progress)**.

---

## 📋 Requirements Compliance Checklist

### 1. Core Features (Giai đoạn 1 - Pass Grade)
- [x] **File Selection:** Users can upload any file type (max 10 MB) via a drag-and-drop zone or file picker.
- [x] **Client Validation:** Automatically rejects uploads exceeding the 10 MB limit and performs local MIME checking.
- [x] **Unique Share Link:** Returns a short unique code (e.g. `f/c03264ab`) and automatically copies the absolute link to the uploader's clipboard with active toast feedback.
- [x] **Upload Limits Controls:** Uploader can restrict access by setting maximum download limits (e.g., 1, 5, 10, 50 downloads) or an expiry timeline (1 hour, 1 day, 7 days).
- [x] **Image Previews:** Inline image rendering for `JPEG`, `PNG`, `GIF`, and `WebP` files directly in the browser. Renders premium type icons for non-previewable formats (e.g., PDFs, Zip archives, Audio, Video).
- [x] **Upload History:** Anonymous history list page leveraging `localStorage` to persist file codes uploaded in the current browser session.
- [x] **REST Bindings:** Connected to backend REST endpoints (`POST /files`, `GET /files/{code}`, `DELETE /files/{code}`).

### 2. Upload Progress (Giai đoạn 2 - Merit Grade)
- [x] **Real-Time Tracking:** Showcases a dynamic progress bar hooked directly into the network layer via `XMLHttpRequest.upload.onprogress` events.
- [x] **Mock Mode Animations:** Simulates smooth incremental progress steps (0% to 100%) in development when the backend is offline.

---

## 🛠️ Architecture & Integration Strategy

### CORS Resolution (Vite Reverse Proxy)
Since the C# backend API is hosted at `http://localhost:7001` without CORS headers enabled, direct browser requests fail. We circumvent this purely in the frontend configuration within `vite.config.js` by mapping all `/files` routes to the backend:
```javascript
server: {
  proxy: {
    '/files': {
      target: 'http://localhost:7001',
      changeOrigin: true,
      secure: false
    }
  }
}
```

### Metadata Resolution from Binary Payload
Because the C# backend lacks a dedicated JSON metadata route (providing only raw binary file downloads via `GET /files/{code}`), the frontend resolves metadata on-the-fly:
1. Fetches the binary file stream as a `Blob`.
2. Reads the `Content-Type` and `blob.size` parameters.
3. Decodes the original filename from the `Content-Disposition` attachment header.
4. Matches the file code with the uploader's local history list to overlay remaining downloads or expiry timestamps.

---

## 🚀 How to Run the Frontend

Make sure you have Node.js (v18+) installed.

### 1. Install Dependencies
```bash
npm install
```

### 2. Run the Development Server
```bash
npm run dev
```
Open **`http://localhost:5173/`** in your browser.

*Note: You can switch between **Mock Mode** (using mock files saved to LocalStorage) and **Real API** (connecting to the backend server) by clicking the badge in the top-right corner of the Navbar.*

### 3. Production Build & Linting
```bash
# Build optimized assets into /dist
npm run build

# Preview production build locally
npm run preview

# Run ESLint validation
npm run lint
```
