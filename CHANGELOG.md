# Changelog

Notable changes to the code. Dates are GST. Documentation-only changes are tracked in `PROJECT_DECISIONS.md` and `docs/KNOWLEDGE_BASE.md`.

## 2026-11-27 — Staff app: fixes from the first phone test

Tested on a real phone (Android 17) against a local API with throwaway data: sign-in, Home, scan by typed registration, start trip (all steps), swap vehicle, end trip + summary, cancel, hand-over, transfer depart/arrive, notifications, lots, trips list, change password, log out / sign in. Fixed:
- Icons: switched from `material_symbols_icons` (most outline icons went blank in release builds) to Flutter's built-in rounded icons; APK stays ~66 MB.
- Grey screen after ending a trip: the summary now loads the trip by id instead of receiving it through navigation; the start flow returns Home if its data is lost.
- Release builds now log UI errors to the device log and show a readable message instead of a grey box.
- First launch no longer says "Your session has ended".
- Segmented controls and scanner buttons no longer use amber (reserved for "live"); scanner close/torch sit at the top.
- My trips now lists trips you handed over (still running under someone else) with "Handed over".
- Transfer screen: driving time shows only after arrival (the live timer covers the trip).
19 tests pass.

## 2026-11-27 — Staff app, first cut (D-053–D-055)

- New `app/` Flutter workspace: `packages/dm_core` (API client with persistent session cookie, error envelope, models, repositories, Emerald Ops theme light/dark, shared widgets) and `apps/staff` ("DM Staff", `com.dm.vms.staff`).
- Staff screens: sign-in, Home (no trip / running / overdue), QR scanner with typed-registration fallback, start trip (vehicle, destination, signature, review), active trip, end trip (verify, return lot, confirm, summary), swap vehicle / hand over, cancel, my trips + detail, lots, transfer task (depart / arrive), notifications, profile + change password + theme.
- Toolchain pinned to the owner's setup (Flutter 3.35.3, AGP 8.7.2, Gradle 8.10.2, SDK 36). 17 tests pass; `flutter analyze` clean; release APK builds.

## 2026-11-27 — Cold-start wait

- During startup, API requests now wait up to 15 s (`Database:StartupWaitSeconds`) for the database check before answering `503 DATABASE_NOT_READY`. Found on the server: the first request after IIS restarted the app got 503. DEPLOYMENT.md §3.3 now also lists *Idle Time-out = 0* and *Preload Enabled*. 107 tests pass.

## 2026-11-27 — Server settings file (D-052)

- Server settings go in `appsettings.Production.json` in the site folder; the file is git-ignored and excluded from publish, so deploys never overwrite it. Production connects with `sa` (owner choice, risk accepted). DEPLOYMENT.md §3.3 has the template.

## 2026-11-27 — Plain-text passwords (D-051)

- `Users.PasswordHash` replaced by `Users.Password` (plain text, nvarchar(128)); migration `PlainTextPasswords`. Existing accounts must have their password set again.
- Passwords compared in code, constant-time and case-sensitive. Still never returned, logged or audited.
- DEPLOYMENT.md §3.4: SQL script to create the first admin and to set a password. 106 tests pass.

## 2026-11-27 — Controllers in one folder (D-049)

- API controllers moved to `Controllers/` (one per file, `AdminsController` now separate). Business logic stays in `Features/<Area>`. No route or behaviour change; 105 tests pass.

## 2026-11-27 — Backend M4–M6: transfers, notifications, reports, release

- **Tables** (migration `TransfersAndNotifications`): Transfers (one open transfer per vehicle), ReminderLogs, Notifications.
- **Transfers (M4):**
  - Admin creates and cancels (before departure).
  - Any staff member confirms departure and drives it; any staff member confirms arrival.
  - The vehicle counts at the origin until arrival.
  - Driving a transfer blocks trips and hand-overs (D-026), and a vehicle with an open transfer can't start a trip or be moved by hand.
  - Driving time counts in staff hours.
- **Notifications and reminders (M5):**
  - In-app notification list (read, read-all).
  - Reminder job every minute (driver plus admins; first after the delay setting, then every 30 minutes; each sent once).
  - Admins are notified on trip cancel and vehicle swap-out.
  - Push stays off (D-039).
  - Trip detail shows the reminder log to admins.
- **Admin views (M5):** dashboard, reports R-1 to R-5 (hours split by GST day), audit-log view, and vehicle history (moved here by D-045).
- **Release (M6):**
  - Folder publish profile (framework-dependent) and `web.config` (IIS passes JSON errors through; 21 MB request filter).
  - Development settings are excluded from publish.
  - `DEPLOYMENT.md` runbook.
- **Refactor:** app locks and the "staff is busy" rule are shared by trips and transfers.
- **Tests:** 105 passing (11 new).

## 2026-11-27 — Staff have no assigned lots (D-047)

- Removed lot assignment: `StaffLots` table dropped (migration `RemoveStaffLots`); `lotIds`/`lots` removed from staff create, edit and responses; `lotId` filter removed from the staff list; `assignedLots` removed from `auth/me`.
- Trips: any staff member can start a vehicle parked at any active lot; `originLotId` removed from `POST trips`; error code `LOT_NOT_ASSIGNED` removed.
- Tests: 94 passing (lot-assignment tests replaced by "start from any active lot, not an inactive one" and a staff-edit test).

## 2026-11-27 — Backend M3: trips

- **Tables** (migration `Trips`): Trips, TripSegments, with filtered unique indexes for one active trip per staff member and per vehicle.
- **Endpoints:**
  - `GET vehicles/lookup` (staff): vehicle card from a QR token or typed registration, with whether and why it can't be started.
  - `POST trips` (staff): start with destination and signature (PNG, stored as a file); vehicle becomes Active and leaves the lot's count.
  - `GET trips/active`, `GET trips`, `GET trips/{id}`, `GET trips/live` (admin, overdue first).
  - `POST trips/{id}/swap-vehicle`, `/handover`, `/cancel`, `/end` (staff, own active trip); end confirms the vehicle and warns (never blocks) on a full lot.
  - `GET staff/handover-candidates` (staff), `GET staff/{id}/trips` with hours (admin).
  - `auth/me` adds `hasActiveTrip` and this month's trips and hours; staff can open signatures of their own trips.
- **Concurrency:** actions are serialised with SQL Server application locks; tested with two phones starting the same vehicle at once.
- **Tests:** 95 passing (17 new).

## 2026-11-27 — Backend M2: master data

- **Tables** (migration `MasterData`): VehicleCategories, VehicleTypes, Vehicles, ParkingLots, LotTypeCapacities, Areas, Locations, StaffLots, AuditLogs.
- **Endpoints:**
  - Categories and types: list (A,S), create, edit, deactivate.
  - Vehicles: list with filters (incl. "Not at a lot"), detail, create (random 22-character QR token), edit, status, location, photo upload (20 MB), QR label data.
  - Lots: list and detail with live occupancy per type and in total (A,S), create, edit (capacities and baselines), deactivate.
  - Destinations: areas and locations (A,S read; A write).
  - Staff: list, detail, create (Staff ID generated), edit (assigned lots), deactivate, reset password, photo.
  - Admins: list, detail, create (Admin ID generated), edit, deactivate (not self), reset password.
  - Settings: Staff ID prefix and first-reminder delay.
  - `auth/me` now includes `assignedLots`.
- **Rules:** registration numbers unique ignoring case, spaces and dashes; vehicle status can't change while out (D-043); `Active` is never set by hand; inactive records hidden from staff; `rowVersion` checks on every edit of shared records (409 on conflict).
- **Audit:** every admin change to master data and accounts is written to `AuditLogs` (who, when, action, changed fields before/after) in the same transaction; password hashes and security stamps are never written.
- **Sessions:** a deactivated account now gets `ACCOUNT_DEACTIVATED` (not `UNAUTHENTICATED`) on its next request, even though deactivation also rotates the security stamp.
- **Tests:** 78 passing (27 new).

## 2026-11-27 — Unknown routes answer 404 to everyone (D-044)

- Sign-in is now required through `MapControllers().RequireAuthorization()` instead of a fallback authorization policy, so a URL that matches no endpoint answers `404 ROUTE_NOT_FOUND` (and a wrong method `405`) to signed-out callers too. Existing endpoints still answer `401` when signed out.
- Tests: 51 passing (added signed-out 404, 405 and 401 cases).

## 2026-11-27 — Backend M1: skeleton and cross-cutting

Added the API solution in `backend/` (D-027, D-028, D-042):

- **Solution:** `VehicleManagementDM.sln`, ASP.NET Core on .NET 8 (SDK pinned to 8.0.202 via `global.json`), `VehicleManagementDM.Api` + `VehicleManagementDM.Api.Tests` (xUnit). Packages: EF Core SQL Server 8.0.34, Swashbuckle 6.9.0 (Swagger in Development only).
- **Database:** `AppDbContext` with Users, IdSequences, AppSettings, ErrorLogs; migration `InitialCreate`. Startup creates the database if missing, applies migrations and seeds the first admin from `Seed:FirstAdmin:*`; retries in the background while the database is unreachable (D-030).
- **Errors:** one JSON error body (`application/problem+json`: status, code, title, detail, errors, traceId) for exceptions, validation, malformed JSON, unknown routes, wrong methods, unsupported media types, oversized bodies, auth failures, rate limits and "database not ready". SQL errors translated (duplicates → 409 with field, connection → 503, timeouts → 503, missing tables → 500 schema error). Production hides unexpected-exception text and shows a trace reference (D-032, D-033).
- **Logging:** every error response logged to `ErrorLogs` with its trace ID; falls back to daily JSON-lines files when the database can't be used; 30-day retention job.
- **Auth:** passwords 8–128 characters (D-044); cookie sessions (`vms.auth`: HttpOnly, Secure, SameSite=Strict; 12 h, or 30 days sliding with "remember me"); security-stamp check on every request (deactivation and password change end sessions immediately); lockout after 5 failures for 15 minutes; login rate limit per IP; Data Protection keys persisted and DPAPI-protected (D-031).
- **IDs:** login IDs generated as `DM`/`ADM` + GST year + 5-digit sequence, never reused (D-011, D-037).
- **Files:** `FileStore` (content-checked JPEG/PNG, generated names, temp-then-commit, 20 MB photos / 512 KB signatures) and `GET /api/v1/files/{kind}/{name}` for signed-in users (D-034).
- **Endpoints:** `POST auth/login`, `POST auth/logout`, `GET auth/me`, `POST auth/change-password`, `GET files/{kind}/{name}`, `GET /health`.
- **Request size:** 1 MB default for all requests (Kestrel and IIS).
- **Tests:** 49 passing — error format and logging for every M1 error path, auth (cookie flags, remember me, lockout, deactivation, password change, roles, rate limit), file store and file endpoint, retention job, startup with an unreachable database, and a check that the model has no changes without a migration.
