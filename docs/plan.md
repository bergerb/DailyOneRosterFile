# Implementation Plan: Daily OneRoster File Generator

## Source of Truth
This file (`docs/plan.md`) is the canonical project plan and must be kept current as work is completed.

## Problem Statement
Create a new public-facing website that generates and provides a downloadable "OneRoster" file (version 1.1) daily.

## Proposed Approach
1.  **Backend (C#):**
    - Create a Web API to serve the file download link.
    - Implement a BackgroundService to run daily.
    - Integrate the OneRosterSampleDataGenerator logic to produce the file.
    - Save the generated file to the local filesystem.
2.  **Frontend (React):**
    - Create a simple landing page for dailyonerosterfile.bergerb.net.
    - Display the download link for the latest file.
    - Include the EduPortal logos as specified.
    - Mint a short-lived download token on page load so the file URL is not directly hotlinkable.
3.  **Infrastructure (Docker):**
    - Configure Docker Compose to run a single integrated app container that serves both backend APIs and frontend static assets.
    - Use the existing bergerb-infrastructure as a reference.
4.  **Testing:**
    - Implement integration tests to verify the end-to-end flow: BackgroundWorker -> File Generation -> API Availability.

## Todo List
- [x] **Backend Project Initialization**
- [x] **Frontend Project Initialization**
- [x] **Backend Development**
    - [x] Implement OneRosterFileGenerator logic (integrate SampleDataGenerator).
    - [x] Implement DailyFileGenerationWorker (ensure 24h interval logic).
    - [x] Create API endpoint for file download.
    - [x] Configure file storage paths and environment variables.
- [ ] **Frontend Development**
    - [x] Build landing page UI.
    - [x] Integrate EduPortal logos.
    - [x] Connect frontend to backend download API.
    - [x] Add short-lived signed download token flow for the file endpoint.
- [ ] **Infrastructure & Deployment**
    - [x] Create integrated `Dockerfile.API` to build frontend and backend into one runtime image.
    - [x] Update `docker-compose.yml` to run a single `app` service for the integrated image.
    - [x] Configure backend static file hosting from `wwwroot` for frontend delivery.
    - [ ] Verify integrated image build and runtime behavior on a machine with Docker daemon running.
- [ ] **Integration Tests**
    - [ ] Write integration test for the file generation worker.
    - [ ] Write integration test for the API endpoint.
    - [ ] Run tests in a CI/CD or local test environment.

## Notes
- The project is a "OneRoster 1.1" generator.
- Local filesystem storage is acceptable as it's a single-node Docker setup.
- Graphics source: ../NemesisApp.
- Integration tests should verify that a file is successfully created and then reachable via the API.
- Deployment model is integrated: one container, one exposed app port (`5000`), shared `generated_files` volume.
- Hotlink protection should use a short-lived signed token, not JWT auth or a public bearer token.
