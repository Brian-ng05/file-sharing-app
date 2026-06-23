# StorageService.Api - AWS S3 Integration

## Cấu hình AWS S3

Dự án hỗ trợ 2 cách cấu hình an toàn: **User Secrets** (cho development) hoặc **Biến môi trường** (cho production).

### Cách 1: Sử dụng User Secrets (Khuyến nghị cho Development)

1. Mở terminal trong thư mục `StorageService.Api`
2. Đặt các giá trị AWS với credentials của riêng bạn:
   ```bash
   dotnet user-secrets set "AwsSettings:AccessKey" "YOUR_ACCESS_KEY"
   dotnet user-secrets set "AwsSettings:SecretKey" "YOUR_SECRET_KEY"
   dotnet user-secrets set "AwsSettings:BucketName" "YOUR_BUCKET_NAME"
   dotnet user-secrets set "AwsSettings:Region" "YOUR_REGION"
   ```

### Cách 2: Sử dụng Biến Môi trường (Khuyến nghị cho Production)

1. Copy file `.env.example` thành `.env`
2. Điền các giá trị AWS của bạn vào file `.env`
3. Đặt các biến môi trường hoặc sử dụng thư viện đọc file .env như `DotNetEnv`

Các biến cần đặt:
- `AwsSettings__AccessKey`
- `AwsSettings__SecretKey`
- `AwsSettings__BucketName`
- `AwsSettings__Region`

(Lưu ý: Dùng `__` thay vì `:` cho biến môi trường)

### Chạy dự án:
```bash
dotnet run
```

### Truy cập Swagger UI:
http://localhost:5282/swagger

## Endpoints:
- `POST /api/s3test/upload`: Upload file lên S3
- `GET /api/s3test/url/{key}`: Lấy pre-signed URL để xem file
