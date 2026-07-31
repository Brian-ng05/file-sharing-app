FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore
COPY MaintenanceService.Api/MaintenanceService.Api.csproj MaintenanceService.Api/
RUN dotnet restore MaintenanceService.Api/MaintenanceService.Api.csproj

# Copy all source and publish
COPY . .
RUN dotnet publish MaintenanceService.Api/MaintenanceService.Api.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MaintenanceService.Api.dll"]
