# File Sharing App

A full-stack file sharing application with React frontend and .NET backend, using AWS S3 for storage.

## Architecture

- **Frontend**: React + Vite
- **Backend**:
  - `FileService.Api`: Manages file metadata, handles user requests (port 7001)
  - `StorageService.Api`: Microservice for S3 storage operations (port 7002)
  - `ApiGateway`: API Gateway using Ocelot (port 5000)
- **Database**: SQL Server
- **Storage**: AWS S3

## Getting Started

### Prerequisites

- .NET 10 SDK
- Node.js 20+
- SQL Server (or SQL Server LocalDB)
- AWS Account with S3 bucket

### Backend Setup

1. **Configure StorageService.Api**
   - Copy `backend/StorageService.Api/appsettings.json` to `backend/StorageService.Api/appsettings.Development.json`
   - Fill in your AWS credentials:
     ```json
     {
       "AwsSettings": {
         "AccessKey": "YOUR_AWS_ACCESS_KEY",
         "SecretKey": "YOUR_AWS_SECRET_KEY",
         "BucketName": "amd201-file-sharing-699592747745-ap-southeast-2-an",
         "Region": "ap-southeast-2"
       },
       ...
     }
     ```

2. **Configure FileService.Api**
   - Verify `backend/FileService.Api/appsettings.json` has correct `StorageService:BaseUrl` (should be `http://localhost:7002`)
   - Update `ConnectionStrings:Default` with your SQL Server connection string

3. **Run Migrations** (make sure SQL Server is running first!)
   ```bash
   cd backend/FileService.Api
   dotnet ef database update
   ```

4. **Run Backend Services**
   - Run StorageService.Api:
     ```bash
     cd backend/StorageService.Api
     dotnet run
     ```
   - Run FileService.Api:
     ```bash
     cd backend/FileService.Api
     dotnet run
     ```
   - (Optional) Run ApiGateway:
     ```bash
     cd backend/ApiGateway
     dotnet run
     ```

### Frontend Setup

1. Install dependencies:
   ```bash
   cd frontend
   npm install
   ```

2. Start development server:
   ```bash
   npm run dev
   ```

## Important Notes

- **Never commit** `appsettings.Development.json` files - they contain sensitive credentials! They are already in `.gitignore`.
- `appsettings.json` files contain default/empty values and are safe to commit.

