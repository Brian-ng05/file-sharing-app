# DropShare - File & Image Sharing Service

DropShare là dịch vụ chia sẻ file và hình ảnh trực tuyến (tương tự WeTransfer và Imgur), được phát triển dựa trên cấu trúc **ASP.NET Core Web API (Backend)** và **React + TypeScript SPA (Frontend)**.

Dự án hiện tại đã hoàn thành tích hợp và kết nối thành công giữa **Frontend (Phase 2)** và **Backend (Nhánh `feature/file-upload`)**.

---

## 👥 Phân Chia Vai Trò (Team Roles)

* **Member 1 (Backend Core):** Đảm nhiệm thiết kế cơ sở dữ liệu EF Core, Repository pattern, API tải lên (`POST /files`), tải xuống (`GET /files/{code}`), và xóa file (`DELETE /files/{code}`).
* **Member 2 (Frontend Domain - Hiện tại đã hoàn thành):** Đảm nhiệm xây dựng giao diện React SPA, kéo-thả file, hiển thị thanh tiến trình thực tế bằng `XMLHttpRequest` (Phase 2), trang Lịch sử tải lên (`localStorage`), trang chi tiết xem trước (Preview) hình ảnh/tài liệu và xử lý tích hợp API.
* **Member 3 (Platform):** Đảm nhiệm lưu trữ AWS S3, ImageSharp Thumbnail, Docker, CI/CD và dịch vụ dọn dẹp chạy ngầm (Background Cleanup Service).

---

## 📁 Cấu Trúc Thư Mục (Folder Structure)

```text
file-sharing-app/
├── backend/                  # Mã nguồn ASP.NET Core Web API
│   ├── ApiGateway/           # Gateway định tuyến (Cổng 5000)
│   ├── FileService.Api/      # API dịch vụ File chính (Cổng 7001)
│   └── ...
├── frontend/                 # Mã nguồn React SPA (Vite + TS) (Cổng 5173)
│   ├── src/
│   │   ├── app/              # Cấu hình Router định tuyến chính
│   │   ├── components/       # Các component dùng chung (UploadForm, Layout, v.v.)
│   │   ├── pages/            # Các trang giao diện (Upload, History, Preview)
│   │   ├── services/         # Tích hợp API và Quản lý Lịch sử
│   │   └── types/            # Định nghĩa kiểu TypeScript
│   └── vite.config.js        # Cấu hình Proxy chống lỗi CORS
└── README.md
```

---

## 🛠️ Hướng Dẫn Cài Đặt & Chạy Dự Án (Quick Start)

### 1. Khởi chạy Backend (.NET Core Web API)
API dịch vụ file chính chạy trên cổng **`http://localhost:7001`**.

1. Cài đặt CSDL SQL Server và cấu hình chuỗi kết nối (Connection String) trong `backend/FileService.Api/appsettings.json`.
2. Mở dự án bằng Visual Studio hoặc chạy lệnh qua terminal:
   ```bash
   cd backend/FileService.Api
   dotnet run
   ```

### 2. Khởi chạy Frontend (React + Vite)
Ứng dụng chạy trên cổng **`http://localhost:5173`**.

1. Di chuyển vào thư mục frontend và cài đặt thư viện:
   ```bash
   cd frontend
   npm install
   ```
2. Chạy ứng dụng trong môi trường phát triển:
   ```bash
   npm run dev
   ```

---

## ⚙️ Giải Pháp Tích Hợp Frontend & Backend

### Chống lỗi CORS bằng Vite Proxy
Do máy chủ Backend (`localhost:7001`) chưa bật CORS, trình duyệt sẽ chặn các kết nối trực tiếp từ Frontend (`localhost:5173`). Chúng tôi đã giải quyết triệt để lỗi này bằng cách cấu hình **Vite Reverse Proxy** trong `frontend/vite.config.js`:
* Mọi request gửi tới đường dẫn `/files` sẽ được Vite tự động chuyển hướng (proxy) ngầm về `http://localhost:7001/files` một cách an toàn.

### Xử lý thông tin file xem trước (Preview)
Do Backend chỉ cung cấp API tải file nhị phân trực tiếp (`GET /files/{code}`) và không có endpoint lấy metadata riêng:
1. Frontend sẽ tiến hành tải file nhị phân về dưới dạng `Blob`.
2. Đọc định dạng (`Content-Type`) và dung lượng trực tiếp từ dữ liệu tải về.
3. Giải mã tên file gốc tự động bằng cách bóc tách header `Content-Disposition`.
4. Nếu người xem là người upload, frontend sẽ tự động gộp (merge) thêm thông tin thời gian hết hạn và giới hạn lượt tải được lưu trong Lịch sử trình duyệt (`localStorage`) để hiển thị đầy đủ và chi tiết nhất.

---

## 🔄 Chế Độ Mock (Mock Mode)
Nếu chưa khởi chạy dự án Backend, bạn vẫn có thể kiểm thử toàn bộ tính năng giao diện bằng cách click vào nút **`Mock Mode`** ở góc trên cùng bên phải thanh Navbar:
* **Mock Mode (Bật):** Lưu trữ file ảo dưới dạng chuỗi base64 vào bộ nhớ trình duyệt, giả lập thanh tiến trình upload tăng dần.
* **Real API (Bật):** Kết nối trực tiếp và truyền tải file thật với máy chủ backend cổng 7001 thông qua XHR.
