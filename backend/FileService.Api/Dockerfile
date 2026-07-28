FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore
COPY FileService.Api/FileService.Api.csproj FileService.Api/
RUN dotnet restore FileService.Api/FileService.Api.csproj

# Copy all source and publish
COPY . .
RUN dotnet publish FileService.Api/FileService.Api.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FileService.Api.dll"]
