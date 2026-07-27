# DropShare Frontend

Frontend web client for the AMD201 Topic 3 file sharing service. This app is responsible for the user-facing upload, history, review, password, download, and error-state flows.

## Role Scope

This frontend belongs to Member 2 - Frontend/UI.

Implemented responsibilities:

- Upload page with drag-and-drop/file picker.
- File size and MIME validation.
- Expiry and download limit controls.
- Optional password protection UI.
- Real upload progress bar.
- Share link success state and copy action.
- Upload history using browser localStorage.
- Review page for shared links.
- Password gate for protected files.
- Download and delete actions.
- Friendly loading and error states.
- Mock Mode and Real API Mode switch.
- Responsive UI polish for demo.

## Tech Stack

- React
- Vite
- TypeScript-style `.tsx` components
- React Router
- CSS modules/files using existing DropShare design variables
- XMLHttpRequest for upload progress
- localStorage for mock mode and uploader history

Note: The current frontend does not use Tailwind or TanStack Query yet. API calls are handled through the local service layer in `src/services`.

## Main Routes

| Route | Page | Purpose |
|---|---|---|
| `/` | `UploadPage.tsx` | Upload a file and generate a share link |
| `/history` | `HistoryPage.tsx` | View locally stored upload history |
| `/f/:code` | `ReviewPage.tsx` | Open a shared file link, verify password if needed, download/delete |

## Frontend Folder Structure

```text
frontend/
  src/
    app/
      router.tsx
    components/
      common/
      file/
      layouts/
      upload/
    pages/
      UploadPage.tsx
      HistoryPage.tsx
      ReviewPage.tsx
    services/
      api.ts
      file.service.ts
      history.service.ts
      storage.service.ts
    types/
      file.ts
```

## API Integration

The frontend has been updated to match the current backend contract.

### Upload

Endpoint:

```text
POST /files
```

Request format:

```text
multipart/form-data
```

Fields sent by the frontend:

- `file`: selected file.
- `maxDownloads`: optional download limit.
- `expiresAt`: optional ISO datetime.
- `password`: optional password for password-protected files.

Expected backend response:

```json
{
  "code": "abc12345",
  "downloadUrl": "/files/abc12345"
}
```

The frontend then creates a share link:

```text
/f/{code}
```

### Metadata / Review Page

Endpoint:

```text
GET /files/{code}/info
```

The review page calls this endpoint when opening `/f/:code`.

Important: the frontend does not call `GET /files/{code}` on page load. This avoids accidentally increasing `downloadCount` when the user only opens the review page.

Expected backend response:

```json
{
  "code": "abc12345",
  "originalFilename": "report.pdf",
  "mimeType": "application/pdf",
  "sizeBytes": 12345,
  "requiresPassword": true,
  "expiresAt": "2026-07-26T12:00:00Z",
  "createdAt": "2026-07-25T12:00:00Z"
}
```

### Password Verification

Endpoint:

```text
POST /files/{code}/verify-password
```

Request body:

```json
{
  "password": "demo123"
}
```

Expected response:

```json
{
  "valid": true
}
```

If `valid` is `false`, the password modal stays open and shows an inline error.

### Download

Endpoint:

```text
GET /files/{code}
```

For password-protected files:

```text
GET /files/{code}?password=demo123
```

The frontend only calls this endpoint when the user clicks the Download button. The backend verifies the password if required, increments `downloadCount`, then redirects to a signed S3 URL.

### Delete

Endpoint:

```text
DELETE /files/{code}
```

After a successful delete, the frontend removes local history and navigates back to `/history`.

## Mock Mode vs Real API Mode

The navbar shows the current mode:

- Yellow dot: Mock Mode
- Green dot: Real API

### Mock Mode

Mock Mode is useful when backend infrastructure is not running. It stores mock file metadata and small mock file content in localStorage.

Mock Mode supports:

- Upload flow.
- Upload progress simulation.
- Share link generation.
- Password-protected mock files.
- Review page.
- Download.
- History.
- Delete.

### Real API Mode

Real API Mode uses the backend through the Vite proxy.

Vite proxy configuration:

```js
server: {
  proxy: {
    "/files": {
      target: "http://localhost:7001",
      changeOrigin: true,
      secure: false
    }
  }
}
```

Required backend service for frontend testing:

- `FileService.Api` running on `http://localhost:7001`

FileService also depends on:

- PostgreSQL database.
- `StorageService.Api` running on `http://localhost:5282`.
- AWS S3 credentials configured safely outside committed source files.

## How To Run

Install dependencies:

```bash
npm install
```

Start development server:

```bash
npm run dev
```

Open:

```text
http://localhost:5173
```

Build production assets:

```bash
npm run build
```

Preview production build:

```bash
npm run preview
```

Lint:

```bash
npm run lint
```

## Manual Test Checklist

### Mock Mode

- Upload a file without password.
- Upload a file with password.
- Set expiry.
- Set download limit.
- Confirm progress bar reaches 100%.
- Copy share link.
- Open `/f/{code}` in a new tab.
- Enter wrong password and confirm inline error.
- Enter correct password and confirm file actions unlock.
- Download file.
- Delete file.
- Check History page.
- Test Copy Link from History.
- Test bulk delete.
- Open an invalid code such as `/f/NONEXISTENT` and confirm friendly error UI.

### Real API Mode

Before testing Real API Mode, make sure backend infrastructure is running.

Required:

- PostgreSQL available to FileService.
- StorageService configured with valid AWS S3 settings.
- FileService running on port `7001`.

Test flow:

- Switch navbar from Mock Mode to Real API.
- Upload without password.
- Upload with password.
- Open generated `/f/{code}` link.
- Confirm Network tab calls `GET /files/{code}/info` on page load.
- Confirm page load does not call `GET /files/{code}`.
- Submit password if required.
- Click Download and confirm `GET /files/{code}?password=...` is called only after clicking Download.
- Delete the file.

## Known Limitations

- Real API end-to-end testing depends on backend infrastructure being available.
- Thumbnail display is prepared as an optional frontend field, but complete thumbnail support depends on backend returning a thumbnail URL.
- Upload history is browser-local. A shared link opened on another device will still show file metadata from the backend, but it will not appear in that device's local upload history.
- The frontend currently keeps the existing CSS approach instead of migrating to Tailwind.
- TanStack Query is not installed; API calls are handled through `file.service.ts`.

## Handoff Notes

Frontend build status:

```text
npm run build: passed
```

Frontend has been aligned with the current backend API contract:

- Upload uses `POST /files`.
- Metadata uses `GET /files/{code}/info`.
- Password verification uses `POST /files/{code}/verify-password`.
- Download uses `GET /files/{code}` only after user action.
- Delete uses `DELETE /files/{code}`.

Real API verification is pending backend infrastructure:

- PostgreSQL setup.
- AWS S3 settings.
- StorageService and FileService running locally or deployed.
