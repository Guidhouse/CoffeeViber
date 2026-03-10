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
- File extension validation (JPG, PNG, GIF, BMP; 10MB max)
- Magic bytes (file signature) validation to reject disguised files
- Kestrel server-level body size limit (11MB transport ceiling)
- Rate limiting: max 10 uploads per IP per minute; returns `429` when exceeded
- Secure file storage outside the web root at `~/.local/share/CoffeeGrind/Uploads/` with GUID file names
- Blazor upload page with API integration and result display
- `.gitignore` covering build artifacts, uploads, secrets, and IDE files

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
# Valid image — should succeed
curl -X POST -F "file=@pictures/test.jpg" http://localhost:5001/api/grind/upload

# Wrong extension — returns 400
curl -X POST -F "file=@somefile.txt" http://localhost:5001/api/grind/upload

# Exceed rate limit — returns 429 after 10 requests in one minute
```

## Security

| Control | Detail |
|---|---|
| Magic bytes check | Server reads file header bytes; extension alone is not trusted |
| Body size limit | Kestrel rejects requests over 11MB before buffering |
| Rate limiting | 10 uploads / IP / minute (fixed window) |
| Safe storage path | Files stored in `~/.local/share/CoffeeGrind/Uploads/`, not in web root |
| CORS | Restricted to `localhost:3001`; must be updated for production |
| Antiforgery | Disabled on this endpoint intentionally — Blazor WASM uses token-based requests |

## Next Step

1. Add image preview and progress bar on frontend
2. Add persistence for upload metadata
3. Add actual grind analysis/comparison logic
3. Add actual grind analysis/comparison logic
