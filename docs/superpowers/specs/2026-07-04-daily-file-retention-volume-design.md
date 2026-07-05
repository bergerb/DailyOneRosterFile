# Daily File Retention + Volume Design

## Goal
Persist generated OneRoster ZIPs in a Docker volume and enforce retention of exactly one ZIP file (latest only).

## Scope
- Backend worker generation + cleanup behavior
- Shared storage path alignment between worker and API download endpoint
- Docker compose volume mount contract

## Non-Goals
- Multi-file history retention
- New storage backends (blob/object store)
- UI changes

## Architecture
Use a single filesystem path (`/app/GeneratedFiles` in container) as canonical storage for generated ZIPs.  
`DailyFileGenerationWorker` generates a new ZIP, then performs retention cleanup in the same directory by deleting all older ZIP files and keeping only the just-generated file.  
`FilesController` reads ZIPs from the same canonical directory and serves the latest file.

## Components and Responsibilities

### 1) Storage Path Configuration
- Introduce a single configured storage path used by both:
  - `OneRosterFileGenerator` / worker flow (write + cleanup)
  - `FilesController` (read latest for download)
- Default to `GeneratedFiles` for local development and support `/app/GeneratedFiles` in container via config/env.

### 2) Generation + Retention Workflow
1. Worker starts scheduled cycle.
2. Generator creates daily ZIP and returns the output file path.
3. Worker scans storage directory for `*.zip`.
4. Worker deletes all ZIP files except the generated one.
5. Worker logs generation result and cleanup summary.

Retention rule: **keep exactly one ZIP total (latest generated file)**.

### 3) Download API Behavior
- `GET /api/files/latest-oneroster` reads from canonical storage path.
- If no ZIP exists: return `404`.
- If one ZIP exists: return that ZIP.

## Data Flow
1. Background worker triggers generation.
2. ZIP written into volume-backed storage path.
3. Cleanup removes older ZIPs.
4. API reads same storage path and streams latest ZIP.

## Error Handling
- If generation fails:
  - Log error.
  - Do not delete existing ZIP(s).
  - Continue to next scheduled run.
- If cleanup delete of old files fails:
  - Log error with file path.
  - Keep service alive (best-effort cleanup).
  - Never delete the newly generated ZIP in cleanup pass.

## Docker Contract
- Compose defines named volume `generated_files`.
- Volume mounts to `/app/GeneratedFiles` in app service.
- App service uses this path for both write and read operations.

## Testing Strategy
1. Unit test (or focused integration test) for retention logic:
   - Given multiple ZIPs and a new file path, cleanup leaves exactly one ZIP (new file).
2. Worker flow test:
   - Generation invoked once and cleanup applied.
3. API test:
   - Endpoint returns `404` when none exist.
   - Endpoint returns ZIP when one exists.
4. Compose configuration validation:
   - Ensure volume maps to `/app/GeneratedFiles`.

## Success Criteria
- After each worker run, storage contains exactly one ZIP file.
- API endpoint serves that ZIP successfully.
- Storage persists across container restarts due to named volume mount.
