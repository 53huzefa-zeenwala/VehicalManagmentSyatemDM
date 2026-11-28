# Vehicle & Parking Management System

One mobile app (Flutter, Android and iOS) with **Staff** and **Admin** roles, on an ASP.NET Core (.NET 8) API and a SQL Server database.

- **Staff (the drivers)** scan a vehicle's QR code, take it out of a parking lot, and bring it back. A live timer runs the whole time.
- **Admins** manage vehicles, staff, lots and transfers, and monitor everything live and in reports.

**Status:** requirements done (v1.1); UX structure and visual design ("Emerald Ops") in review. Backend API in progress: the backend API is complete (milestones M1–M6: auth, master data, trips, transfers, notifications, dashboard, reports) and tested; deployment steps are in [`DEPLOYMENT.md`](DEPLOYMENT.md). No Flutter code yet.

## Start here

| File | What |
|---|---|
| [`docs/KNOWLEDGE_BASE.md`](docs/KNOWLEDGE_BASE.md) | **Everything in one place**: current rules, roles, screens, design, decisions, open items, environment |
| [`PROJECT_DECISIONS.md`](PROJECT_DECISIONS.md) | Decision log. Source of truth |
| [`ARCHITECTURE.md`](ARCHITECTURE.md) · [`DATABASE_DESIGN.md`](DATABASE_DESIGN.md) · [`API_DOCUMENTATION.md`](API_DOCUMENTATION.md) | Backend structure, tables, endpoints and error codes |
| [`CHANGELOG.md`](CHANGELOG.md) | What changed in the code |
| [`DEPLOYMENT.md`](DEPLOYMENT.md) | How to publish and run the API on the server, backups, go-live checklist |
| [`REQUIREMENTS.md`](REQUIREMENTS.md) | Functional requirements v1.1 (§0 = latest changes) |
| [`docs/design/mockups/UI_Mockups_v1.pdf`](docs/design/mockups/UI_Mockups_v1.pdf) | UI mock-ups: 24 screens in light and dark |
| [`docs/ux/UX_ARCHITECTURE.md`](docs/ux/UX_ARCHITECTURE.md) | Navigation, screen inventory, journeys, states |
| [`docs/design/DESIGN_SYSTEM.md`](docs/design/DESIGN_SYSTEM.md) | Colours, type, components |
| [`DEVELOPMENT_ROADMAP.md`](DEVELOPMENT_ROADMAP.md) | Phase status |
| [`OPEN_QUESTIONS.md`](OPEN_QUESTIONS.md) | Every question round with the client and owner |
| [`CLAUDE.md`](CLAUDE.md) | Working rules for the AI assistant |

## Backend API (`backend/`)

ASP.NET Core on .NET 8 (SDK pinned in `backend/global.json`), EF Core code-first migrations, SQL Server.

**One-time setup on a dev machine** (values stay in `dotnet user-secrets`, never in git — this repo is public):

```powershell
cd backend
dotnet tool restore
dotnet user-secrets set "ConnectionStrings:Default" "<dev database connection string>" --project src/VehicleManagementDM.Api
dotnet user-secrets set "Seed:FirstAdmin:FullName" "<name>" --project src/VehicleManagementDM.Api
dotnet user-secrets set "Seed:FirstAdmin:Mobile" "<+971...>" --project src/VehicleManagementDM.Api
dotnet user-secrets set "Seed:FirstAdmin:Password" "<8+ characters>" --project src/VehicleManagementDM.Api
dotnet user-secrets set "TestDatabase:ConnectionString" "<server connection string, any database>" --project tests/VehicleManagementDM.Api.Tests
```

**Run:** `dotnet run --project src/VehicleManagementDM.Api --launch-profile https` → Swagger at `https://localhost:7181/swagger`. On startup the API creates the database if missing, applies migrations, and creates the first admin if none exists (its login ID is written to the log).

**Test:** `dotnet test` — integration tests create a throwaway `VehicleManagementDM_Test_<guid>` database on the test server and drop it afterwards.

**After changing an entity or configuration:** `dotnet ef migrations add <Name> --project src/VehicleManagementDM.Api --output-dir Data/Migrations`, then commit the migration with the change. A test fails if a model change has no migration.

## Mobile apps (`app/`)

Flutter pub workspace (D-053): `packages/dm_core` (API client, session, models, theme, shared widgets and screens) and one folder per app: `apps/staff` (DM Staff) and `apps/admin` (DM Admin). Android only for now.

**Toolchain (pinned, D-055):** Flutter 3.35.3 stable (don't `flutter upgrade`), Android Studio JBR 21 as `JAVA_HOME`, Android SDK platform 36 + build-tools 36.0.0 + NDK 28.2.13676358, Android Gradle Plugin 8.7.2, Gradle 8.10.2, Kotlin 2.1.20. Newer Android plugin versions need AGP 8.9+, so `app/pubspec.yaml` pins a few of them.

**One-time setup:**
- Flutter is not on PATH; prepend it per shell: `$env:Path = "E:\downloads\flutter_windows_3.35.3-stable\flutter\bin;" + $env:Path`
- `$env:PUB_CACHE = 'E:\flutter_pub_cache'`
- `app/apps/staff/android/local.properties` and `app/apps/admin/android/local.properties` (not in git): `sdk.dir=` your Android SDK, `flutter.sdk=` your Flutter folder.
- Copy `app/dart_defines/staff.example.json` to `staff.json` and `admin.example.json` to `admin.json`, and set `API_BASE_URL` (server origin, e.g. `https://<server>:8445`). The real files are git-ignored.

**Commands** (from `app/`):

```powershell
flutter pub get
flutter analyze apps/staff apps/admin packages/dm_core
cd packages/dm_core; flutter test; cd ../..
cd apps/staff; flutter test
flutter build apk --release --obfuscate --split-debug-info=build/debug-symbols --dart-define-from-file=../../dart_defines/staff.json
cd ../admin; flutter test
flutter build apk --release --obfuscate --split-debug-info=build/debug-symbols --dart-define-from-file=../../dart_defines/admin.json
```

Each APK is `app/apps/<app>/build/app/outputs/flutter-apk/app-release.apk`: one universal APK for every phone type, signed with the debug keystore (sideloading only; a real keystore is needed for Play Store). Keep `build/debug-symbols` to read crash stack traces from the obfuscated build.

## Re-render the mock-ups

Needs macOS with Google Chrome, Python 3 with Pillow, and an internet connection (for Google Fonts).

```bash
python3 -I tools/render_mockups.py
```

This renders `prototype/ui-mock/screens.html` (the dummy app) to PNGs in `docs/design/mockups/png/` and rebuilds the PDF. Use `--check-only` to only check layouts for overflow, or `--deck-only` to rebuild the PDF from existing PNGs.

## Layout

```
docs/source/      client inputs (original PDF, clarifications, staff mock-up, palette)
docs/ux/          UX architecture + low-fi wireframes
docs/design/      design system, mock-up PDF and PNGs
prototype/ui-mock dummy app used for the mock-ups
tools/            render script
backend/          API solution (src/VehicleManagementDM.Api, tests/VehicleManagementDM.Api.Tests)
```
