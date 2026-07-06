# DailyOneRosterFile

DailyOneRosterFile is a robust system designed to automate the generation and management of OneRoster data files. It provides a .NET Web API backend for processing data into OneRoster-compliant `.zip` archives and a modern React frontend for user interaction.

## Features

- **Automated Generation**: Automatically generates OneRoster zip files based on sample data and configurations.
- **Flexible Storage**: Support for both local file system storage and MinIO (S3-compatible) object storage.
- **Secure Downloads**: Implements a custom signed-token mechanism for secure, time-limited file downloads.
- **Modern Frontend**: A clean, responsive React interface built with TypeScript and Vite.
- **Docker Ready**: Includes `Dockerfile` and `docker-compose` support for easy deployment.

## Tech Stack

- **Backend**: .NET (C#) Web API
- **Frontend**: React 19, TypeScript, Vite
- **Storage**: MinIO, Local Filesystem
- **Authentication**: HMAC-based Token System

## Getting Started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/)
- (Optional) [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Backend Setup

1. Navigate to the root directory.
2. Update `appsettings.json` with your configuration (Storage path, MinIO credentials, etc.).
3. Run the project:
   ```bash
   dotnet run --project src/DailyOneRosterFile.Api.csproj
   ```

### Frontend Setup

1. Navigate to the `client` directory:
   ```bash
   cd client
   ```
2. Install dependencies:
   ```bash
   npm install
   ```
3. Start the development server:
   ```bash
   npm run dev
   ```

### Docker Setup

To run the entire stack using Docker:
```bash
docker-compose up -d
```

## Configuration

Configuration is handled via `appsettings.json`. Key sections include:
- `StorageOptions`: Define `UseMinio`, `GeneratedFilesPath`, and `TokenSecret`.

## License

[TBD]
