# Coffee Grind Comparison Project

Coffee grinder image upload MVP with a .NET backend API and a Blazor WebAssembly frontend.

## Simple Structure

```
vibeCopilot/
├── backend/
│   └── CoffeeGrindBackend/
├── frontend/
│   └── CoffeeGrindFrontend/
├── pictures/
└── README.md
```

## Current Status

Implemented:

- Backend upload API (`/api/grind/upload`)
- File validation (JPG, PNG, GIF, BMP; 10MB max)
- Secure storage in `backend/CoffeeGrindBackend/Uploads/` with GUID file names
- Blazor upload page with API integration and result display

## API

- Method: `POST`
- Route: `/api/grind/upload`
- Content-Type: `multipart/form-data`
- Field: `file`
- Max size: 10MB
- Allowed types: JPG, PNG, GIF, BMP

Example success response:

```json
{
  "message": "File uploaded successfully!",
  "fileName": "generated-file-name.jpg",
  "originalName": "input.jpg",
  "fileSize": 129394,
  "fileType": "image/jpeg"
}
```

## Prerequisites

- .NET SDK (latest stable)
- Git

## Build and Run

Build both projects from the repository root:

```bash
dotnet build backend/CoffeeGrindBackend
dotnet build frontend/CoffeeGrindFrontend
```

Backend:

```bash
cd backend/CoffeeGrindBackend
dotnet build
dotnet run
```

Backend URL: `http://localhost:5001`

Frontend:

```bash
cd frontend/CoffeeGrindFrontend
dotnet build
dotnet run
```

Frontend URL: `http://localhost:3001`

## Test Upload with curl

```bash
curl -X POST -F "file=@pictures/test.jpg" http://localhost:5001/api/grind/upload
```

## Next Step

1. Add image preview and progress bar on frontend
2. Add persistence for upload metadata
3. Add actual grind analysis/comparison logic
