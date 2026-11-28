# Project Knowledge Base

**Current state as of 2026-11-27.** This file holds everything known about the project in one place, reflecting every change up to decision D-055. Older documents may still contain superseded lines; when they disagree, this file and `PROJECT_DECISIONS.md` win.

**Reading order for a new session:**
1. This file
2. `PROJECT_DECISIONS.md` (source of truth)
2a. For API work: `ARCHITECTURE.md` (structure, milestones §13) → `DATABASE_DESIGN.md` (tables, location rules) → `API_DOCUMENTATION.md` (endpoints, error codes)
3. `REQUIREMENTS.md` v1.1 (§0 lists the changes)
4. `docs/ux/UX_ARCHITECTURE.md`
5. `docs/design/DESIGN_SYSTEM.md`
6. `docs/design/mockups/UI_Mockups_v1.pdf`

---

## 1. The product in one paragraph

Two mobile apps (Flutter, Android and iOS), **DM Staff** and **DM Admin**, sharing one core package (D-053), on one ASP.NET Core Web API (.NET 8) and one Microsoft SQL Server database. There is no web portal.

- **Staff** take municipal fleet vehicles (cars, garbage trucks, ambulances…) out of parking lots and bring them back. **The staff member is the driver.** The app records who took which vehicle, from which lot, to which destination and for how long. A live timer runs until the vehicle is back.
- **Admins** set up the master data (vehicles, staff, lots, destinations), create transfers between lots, and **monitor** activity live and in reports. They never act on trips themselves.

Client context (inferred): a Dubai municipal fleet. Evidence: "DM" branding and Staff IDs, Dubai locations, +971 numbers. Single organisation, timezone GST (UTC+4), English in v1, with layouts ready for right-to-left (Arabic later).

## 2. Roles

| | Staff (driver) | Admin |
|---|---|---|
| Who | Field staff on their **personal phones**. Each staff member drives the vehicles they take out | Supervisors; more admin accounts can be added. Our team creates the first one |
| Login | Staff ID (`DM` + YY + 5 digits, e.g. `DM2600042`, generated) + password issued by the admin | Admin ID (`ADM` + YY + 5 digits, e.g. `ADM2600001`, D-037) + password |
| Can | Scan a vehicle; start, swap, cancel and end **their own** trips; confirm transfers; see lots and space; see their own trips and profile | Manage vehicles, categories and types, QR labels, staff, admins, lots, destinations and settings; create transfers; see live trips, the dashboard, reports and the audit log; resolve "Not at a lot" vehicles and set vehicle status |
| Cannot | Manage master data; see other people's trips | **End, swap or cancel trips; scan QR codes** (monitor only, D-021) |

## 3. Scope (v1)

**In scope**
- Authentication
- Staff management (with driving licence), admin management
- Vehicle catalogue (Category → Type), QR codes (generate, image or PDF label, bulk print)
- Parking lots (capacity per type, baseline count; no map in v1, D-056), destinations (Area → Location)
- Trips (start, live timer, reminders, swap or hand-over, cancel, end)
- Transfers
- Push notifications
- Admin dashboard, live view, on-screen reports
- Settings, audit log

**Out of scope**
- Web portal; billing and payments; maintenance module (only a manual "Under maintenance" status); fuel logs; live vehicle GPS tracking
- Report exports (PDF/Excel)
- **Separate driver records or drivers logging in** (D-020)
- **Attendance** (check in/out, breaks, photo + GPS, shifts; D-023)
- Admin scanning or admin trip actions (D-021)
- Offline mode; Arabic interface (layouts are RTL-ready); multiple organisations

## 4. Business rules (current)

### 4.1 Trips

**Start** (staff, from any active lot; staff have no assigned lots, D-047). The origin is the lot the vehicle is parked at:
2. Vehicle: scan the QR code, or type the registration number.
3. Destination: area → location.
4. **Staff signature**: the staff member signs to confirm taking the vehicle.
5. Review → Start. The server records the start time.

**Checks at start:**
- The vehicle is Available and located at the origin lot.
- The staff member has no other active trip. Driving an in-transit transfer counts as one (D-026).
- The vehicle is in no other trip or transfer.

**While active:**
- An **amber live bar** with the plate and a ticking timer is pinned on every staff screen. It turns **red** when overdue.
- Reminders: the first goes out after a delay the admin sets, then every 30 minutes, to the staff member **and** the admin.

**Swap** (staff, with a reason; the same trip continues, and a new segment starts):
- **Change vehicle:** the new vehicle must be Available at a lot. For the old vehicle, staff pick the lot where it is or mark "Not at a lot".
- **Hand over:** another staff member takes over the trip, and must have no active trip.

**Cancel** (staff): pick a reason (driver, vehicle, technical or other) and add a note. The trip ends now and **its hours still count**. Staff place the vehicle (a lot or "Not at a lot"). The vehicle's status doesn't change; the admin decides.

**End** (staff, own trip only): scan or type the vehicle (it must match) → choose the return lot (any active lot; the origin is pre-selected; a full lot gives a warning but isn't blocked) → confirm → a summary shows the vehicle, category, from and to lots, start, end, duration, driver (staff) and signature.

**States:** Active → Ended, or Cancelled. There is no "ended by admin" state any more (D-021).

**Stuck trips (D-024, OI-10 closed):** nobody except the staff member can close a trip. If they can't act, the trip stays Active, reminders continue, it stays overdue in the admin's Live tab and "Needs attention", and hours keep growing. Risk accepted by the owner.

### 4.2 Hours

- Hours are counted by segment, for each **staff member** and each **vehicle**. "Driver hours" are the same as staff hours.
- Cancelled trips count.
- Hours that cross midnight are split by calendar day in reports.
- All times come from the server clock, are stored in UTC and shown in GST.

### 4.3 Lots and capacity

- **Fields:** name, unique code, address (no map pin, D-056), contact person and number, opening and closing time (information only), type (Open, Underground or Other), total capacity, capacity per vehicle type, status (Active/Inactive), baseline occupied count.
- **Occupancy:** occupied = baseline + vehicles located at the lot and not on a trip + vehicles in a transfer that hasn't arrived yet. Available = capacity − occupied, per type and in total.
- **Full lot:** a warning, never a block.
- **Inactive lots:** can't be used for start, return or transfer.
- **Map view:** none in v1 (D-056); the Lots list shows free spaces.

### 4.4 Transfers

- The admin creates a transfer: vehicle + destination lot, with an optional note. **No driver is chosen.**
- Any staff member confirms departure (D-047), and **whoever confirms it drives**. Hours start then. They must have no active trip, and while driving the transfer they can't start a trip or receive a hand-over (D-026).
- Any staff member confirms arrival, and hours end.
- The vehicle counts at the origin lot until arrival.
- The admin can cancel a transfer before departure.
- A transfer in transit gets the same reminder rule as trips.

### 4.5 Vehicles, statuses, QR

- **Fields:** registration number (unique), category → type, brand, model, colour, photo.
- **Statuses:** Available · Active (on a trip or transfer) · Inactive · Under maintenance (set by the admin).
- **Location:** a lot, or "Not at a lot" (the admin resolves it).
- **QR codes:** the code holds no readable data. Admins can download an image, a printable PDF label, or bulk-print. A damaged sticker is reprinted with the same code. **Only staff scan**; typing the registration number is the fallback.

### 4.6 Accounts

- Staff profile fields: full name, photo, mobile, optional email, **driving licence number**, Staff ID. **No assigned lots** (D-047).
- The admin resets staff passwords; our team resets the first admin's. There's no forced password change at first login. Users can change their own password.
- Lockout after 5 failed attempts, for 15 minutes.
- Deactivated accounts end their sessions immediately.
- Records are **deactivated, never deleted**.

### 4.7 Reports and dashboard (admin)

- **Dashboard:** live count of vehicles on trip, a trips-per-hour chart, trips today, hours out, free spaces, then "Needs attention" (overdue trips, vehicles not at a lot, lots over capacity, transfers waiting).
- **Live tab:** trips (overdue first) and transfers. No auto-updates: data loads on open and pull to refresh; timers tick on the phone (D-035).
- **Trip detail:** read-only, with staff, vehicle, route, segments and the reminder log.
- **Reports** (5, on screen with filters, no export): vehicle running hours, staff hours and activity (= driver hours), lot utilization, trip log (including cancelled and handed-over trips), transfers.
- **Audit log:** every admin create, edit, deactivate, status change and transfer.

## 5. Navigation and screens

- **Staff tab bar:** Home · Trips · **Scan** (raised centre button) · Lots · **Profile**.
- **Admin tab bar:** Dashboard · Live · Manage · Reports. No scan button.

**Designed screens** (in the PDF, light and dark):
- **Staff:** Home (ready), Scan, Start: Vehicle (1/4), Destination (2/4), Signature (3/4), Review (4/4), Home (trip running), Active trip (overdue, hand-over in the timeline), Swap, End: return lot, Trip summary, Cancel, My trips, Lots (list; map removed, D-056), Profile, Sign in.
- **Admin:** Dashboard, Live, Trip detail (read-only), New transfer, Manage hub, Vehicle detail (resolve "Not at a lot", QR), Lot detail, Report.

**Still to design** (Phase 4): the remaining list and form screens (vehicle, staff, lot, destination and category forms; staff detail; admin management; settings; audit log; notifications; transfer confirm for staff; QR bulk print), plus empty, error, offline and permission states.

## 6. Design system: "Emerald Ops" (D-017, approved)

- **Colours:**
  - Emerald primary `#0B7A62` (dark mode: mint `#34D3A6`)
  - Hero gradient `#06503F → #0B7A62 → #0C7F64`
  - **Amber `#FFB020` = live only** (timer bar, on trip)
  - Danger `#D92D20`, warning `#B54708`, info `#175CD3`
  - Light background `#F3F6F5`; night dark background `#0A1214`
  - All text pairings pass WCAG AA (computed).
- **Type:** Plus Jakarta Sans (with tabular figures for timers). Arabic later: pair with IBM Plex Sans Arabic or Noto Kufi Arabic.
- **Icons:** Material Symbols Rounded in the mock-ups; the Flutter apps use the built-in Material Icons (rounded), because Material Symbols lose icons in shrunk release builds (D-055).
- **Signature elements:** the live bar, UAE-style plate badges, emerald hero cards with route lines, the raised staff Scan button. Status is always an icon + a word.
- **Shape:** cards 22 px radius, buttons 54 px tall / 17 px radius, 4-pt spacing.
- **Alternatives** (last page of the PDF): Ocean (blue + orange), Midnight Lime.
- **Rejected history:** the Instagram palette Ink Heather / Putty / Canary (D-004 → superseded) and "Fleet Signage" (D-015, rejected).

## 7. Decision history

| ID | Decision | Status |
|---|---|---|
| D-001 | Two apps + central backend, no web portal | Superseded by D-007 |
| D-002 | Flutter · ASP.NET Core · SQL Server | Approved |
| D-003 | .NET 10 | Rejected |
| D-004 | Instagram palette | Superseded by D-017 |
| D-005 | UX-first phased delivery (10 phases) | Approved |
| D-006 | Client clarifications batch 1 | Approved |
| D-007 | One app, role from the account | Superseded by D-053 |
| D-008 | .NET 8 (end-of-life risk accepted) | Approved |
| D-009 | Business rules batch 2 | Approved; driver and admin-end parts superseded |
| D-010 | Business rules batch 3 | Approved; driver and attendance parts superseded |
| D-011 | Staff ID format `DM`+YY+5 digits | Approved |
| D-012 | Business rules batch 4 (segments, transfer hours, RTL-ready, on-screen reports) | Approved; Driver ID and attendance parts superseded |
| D-013 | `REQUIREMENTS.md` v1.0 + P-01…P-22 | Approved; P-02 and P-11 superseded |
| D-014 | UX structure UX-01…UX-12 | Approved (2026-11-27) |
| D-015 | "Fleet Signage" design | Rejected |
| D-016 | Review UX and design together on themed screens | Approved |
| D-017 | "Emerald Ops" design | Approved (2026-11-27) |
| D-018 | Deliverables = dummy app → PNG → PDF; knowledge base | Approved |
| D-019 | Scan button in both tab bars | Superseded by D-022 |
| D-020 | **The staff member is the driver**, staff signature kept, swap = vehicle or hand-over, transfer driver = whoever confirms departure | Approved |
| D-021 | **Admins monitor only**, no admin scanner | Approved |
| D-022 | Staff tabs Home·Trips·Scan·Lots·Profile; admin 4 tabs | Approved |
| D-023 | **Attendance removed** | Approved |
| D-024 | **Stuck trips stay open** (OI-10 risk accepted) | Approved |
| D-025 | Development continues on Windows 11 | Approved |
| D-026 | **Driving a transfer = the one active trip** | Approved |
| D-027 | **Start API development now** (ahead of Phases 4–6); owner gives coding style | Approved |
| D-028 | **API = same SDK/template as the Sufizz Bikes backend** (net8.0, VS 2022, MVC template + API controllers, EF Core, JWT, Swagger, folder publish); nothing new on the server | Approved |
| D-029 | **New, empty database** for this app | Approved |
| D-030 | **Code-first migrations**, applied at startup; DB created if missing | Approved |
| D-031 | **Cookie-based authentication** (replaces token wording in REQUIREMENTS §8) | Approved |
| D-032 | **All errors logged + one JSON error format**, never HTML; real message where possible | Approved |
| D-033 | API structure and conventions, `ARCHITECTURE.md` (v0.2); code name `VehicleManagementDM`; Production errors = "Something went wrong" + traceId; error logs kept 30 days | Approved |
| D-034 | **Photos/signatures as files** on the server; DB stores only the API-generated unique name; served only via authenticated API | Approved |
| D-035 | **No auto-refresh**; users refresh manually (supersedes 5 s real-time target) | Approved |
| D-036 | **Self-signed HTTPS**, app accepts every certificate. **Security risk accepted** (pinning declined) | Approved |
| D-037 | **Admin ID** `ADM` + YY + 5 digits | Approved |
| D-038 | Dev + test DBs on the server's SQL Server | Approved |
| D-039 | **Notifications in-app only for now**; push later | Approved |
| D-040 | **QR images and PDF labels built in the app** | Approved |
| D-041 | Hand-over: receiver does nothing | Approved |
| D-042 | **API plan M0–M6**; photos up to **20 MB**, signatures 512 KB | Approved |
| D-043 | **DB design approved.** Cancel/swap-out vehicle → Available + admin notified; first reminder default 120 min; per-type capacities needn't sum to total; status change only when not out | Approved |
| D-044 | Password 8–128 chars; unknown routes 404 for everyone (owner changed from 401) | Approved |
| D-045 | Settings limits (prefix 1–5 letters, delay 1–1,440 min); vehicle history → M4, staff trips and hand-over picker → M3 | Approved |
| D-046 | Trip details from M3: origin = vehicle's lot; swap from any active lot; hand-over to any free staff; cancel note required; overdue = first reminder passed; staff see trips they took part in | Approved |
| D-047 | **Staff have no assigned lots**: start from any active lot, confirm any transfer; `StaffLots` dropped | Approved |
| D-048 | M4–M6 details: transfer rules, notification audiences (all staff / all admins), dashboard and report definitions, deployment layout | Approved |
| D-049 | API controllers in one `Controllers/` folder; logic stays in `Features/<Area>` | Approved |
| D-050 | Hosting: IIS on the app server, ASP.NET Core 8.0.18, port 8445; first admin created by the owner | Approved |
| D-051 | **Passwords stored as plain text** (replaces hashing); compared in code, case-sensitive; risk accepted by the owner | Approved |
| D-052 | Production connects with `sa` (owner choice, risk accepted); settings in `appsettings.Production.json` on the server only | Approved |
| D-053 | Two apps (DM Staff, DM Admin) sharing `app/packages/dm_core`; replaces D-007 | Approved |
| D-054 | Flutter started before all Phase 4 screens; D-014 and D-017 approved | Approved |
| D-055 | Mobile stack and toolchain pins (Flutter 3.35.3, AGP 8.7.2, Gradle 8.10.2, SDK 36; Riverpod, go_router, dio, mobile_scanner) | Approved |

## 8. Phase status

| Phase | Status |
|---|---|
| 0 Requirement clarification | ✅ Done |
| 1 Business analysis | ✅ Done (`REQUIREMENTS.md` v1.1) |
| 2 UX / IA + 3 Design system | ✅ Approved (D-014, D-017; 2026-11-27) |
| 4 High-fidelity UI (remaining screens and states) | After approval |
| 5–6 Architecture, database | Not started; decisions made during API work are logged and written up later (D-027) |
| 7 Backend API | ✅ **M1–M6 done** (2026-11-27): auth, errors, logging, files, master data + audit, trips, transfers, notifications + reminders, dashboard, 5 reports, audit log view, vehicle history; release files and `DEPLOYMENT.md`. 105 tests pass. Deployed 2026-11-27 to IIS on the app server, port 8445 (D-050, D-052); test data seeded |
| 8 Flutter | 🟡 Staff app built and phone-tested (2026-11-27); admin app built (2026-11-28, D-057): every admin screen, QR labels (print, share, save), photos. Next: admin phone test, live-server check |
| 9–10 QA, deployment | Not started |

## 9. Open items

| ID | Item | Needed by |
|---|---|---|
| OI-01 | Scale: vehicles, lots, staff, trips per day | Phase 5 |
| OI-02 | Privacy policy text and retention. Smaller now: no attendance photos or GPS; signatures and phone numbers remain | Phase 7 |
| ~~OI-03~~ | Not needed: no map in v1 (D-056) | Closed |
| OI-04 | App name, icon, logo (placeholder "Fleet & Parking") | Phase 4 |
| OI-05 | App store accounts and distribution | Phase 10 |
| OI-06 | Hosting: the server that already runs the Sufizz Bikes backend (D-028). Its SQL Server is 2019 Express RTM (15.0.2000.5) | Phase 5 |
| OI-07 | Development machine: Windows 11 (D-025). Install missing tools when their phase starts | Phase 7 |
| OI-08 | Tell the client: one app with roles, not two apps; admin views refresh manually, not "real time" (D-035) | Before Phase 4 sign-off |
| OI-09 | .NET 8 is past end of support (10 Nov 2026); revisit | Before Phase 7 |
| OI-10 | ~~Who closes a trip if its staff member can't~~ Closed by D-024 (stays open) | — |
| OI-11 | iOS builds and signing: a Mac or a cloud macOS build service | Phase 8 |
| OI-12 | **Resolved 2026-11-27 (D-052):** database `VehicleManagementDM` on the app server's SQL Server 2019 Express, connected with `sa` by owner choice (risk accepted). Express limits: 10 GB per database, ~1.4 GB buffer memory, 4 cores. Recommended later: a least-privilege login and closing port 1433 to the public | Done |
| OI-13 | **Resolved 2026-11-27 (D-050):** IIS on the app server, ASP.NET Core 8.0.18 runtime, API on port 8445 (HTTPS) | Done |
| OI-14 | ~~HTTPS with domain + real certificate~~ Closed by D-036 (self-signed, app accepts all certificates) | — |

## 10. Environment and tooling

- **Machine (current, D-025):** Windows 11 Pro. Checked 2026-11-26:
  - **Available:** git 2.45, Python 3.12 + Pillow 12.3 (`python` / `py`, no `python3`), Google Chrome, .NET SDK 8.0.202 + 9.0.321 (ASP.NET Core runtime 8.0.3), OpenJDK 21, adb, GitHub CLI 2.102, SQL Server Express (service `MSSQL$SQLEXPRESS`, stopped).
  - **Not found:** Flutter, Docker, `sqlcmd`.
  - The render script and README were written for macOS; verify on Windows before the next render.
- **API reference project (D-028):** `E:\visual studio project\SufizzBikesBackend\`. Use only its SDK, target framework, template and publish method. **Don't copy its code or code style**; the owner gives the coding instructions.
- **DB connectivity (2026-11-26):**
  - The host in the reference connection string is **not reachable** from this machine (TCP 1433 fails, SqlClient timeout).
  - The **app server's own host is reachable** on TCP 1433. Login with the reference credentials succeeded (166 ms): **SQL Server 2019 Express (64-bit), version 15.0.2000.5 (RTM, no cumulative updates)**. The login is `sysadmin`. Only read-only server-property queries were run.
  - So development can connect to the server's SQL Server directly. Still open (OI-12): a dedicated database and a least-privilege login for this app.
- **Flutter toolchain (D-055, owner's working setup):** Flutter 3.35.3 at `E:\downloads\flutter_windows_3.35.3-stable\flutter` (not on PATH; prepend `bin`), `PUB_CACHE=E:\flutter_pub_cache`, `JAVA_HOME` = Android Studio JBR 21, Android SDK at `%LOCALAPPDATA%\Android\Sdk` (platform 36, build-tools 36.0.0, NDK 28.2.13676358). No emulator binary installed; test on a phone over USB (`adb`).
- **Lessons learned (Flutter):**
  - Latest `mobile_scanner` (7.5), `url_launcher_android` and `path_provider_android` pull androidx libraries that need AGP 8.9.1+. With AGP 8.7.2: `mobile_scanner` 7.0.x, and `dependency_overrides` in `app/pubspec.yaml` (url_launcher_android 6.3.14, path_provider_android 2.2.15).
  - Pub cache on E: and project on D: break incremental Kotlin compilation ("Could not close incremental caches"); `kotlin.incremental=false` in `gradle.properties`.
  - PowerShell eats `^` in `flutter pub add 'pkg:^1.0'`; write constraints in `pubspec.yaml` directly.
  - `material_symbols_icons`: most outline icons go blank in release builds (icon-font shrinking). Use built-in `Icons.*_rounded` / `*_outlined`.
  - Release builds show a grey box and log nothing on a widget error: `main.dart` now prints errors to `adb logcat -s flutter` and shows a readable panel.
  - Don't pass objects needed after a router refresh through go_router `extra`; pass an id in the path and load it (trip summary).
  - Phone testing over Tailscale: `adb pair <tailscale-ip>:<pair port> <code>` once, then `adb connect <tailscale-ip>:<port from Wireless debugging>`; the port changes whenever Wireless debugging restarts. `adb reverse tcp:7181 tcp:7181` lets the phone reach the local API as `https://localhost:7181`. Keep *Stay awake* on.
- **Public repo rule:** never write server IPs, database names, logins, passwords or JWT keys into this repository.
- **Backend dev setup (D-038):** secrets live in `dotnet user-secrets` on this PC only:
  - API project: `ConnectionStrings:Default` (server SQL Server, database `VehicleManagementDM_Dev`) and `Seed:FirstAdmin:*` (first admin; its password is only in user-secrets, its login ID is in the startup log).
  - Test project: `TestDatabase:ConnectionString` (server, `master`); tests create and drop `VehicleManagementDM_Test_<guid>`.
  - Currently the `sa` login from the Sufizz Bikes config (owner's choice, temporary until OI-12).
  - Commands: README "Backend API". Run: `dotnet run --project src/VehicleManagementDM.Api --launch-profile https` (from `backend/`), Swagger at `https://localhost:7181/swagger`. Test: `dotnet test`.
- **Lessons learned (backend):**
  - Use `SqlConnectionStringBuilder["Data Source"]` (indexer with the exact keyword) in PowerShell; property-style assignment silently failed once and left the old host in a secret.
  - `WebApplicationFactory` config overrides aren't visible while `Program` registers services; read settings via the options system (lazily), never eagerly.
- **Machine (until 2026-11-26):** Apple Silicon Mac, macOS 15.5, where the project was started. It had git 2.39, Chrome 157, Python 3.14 + Pillow 12.2, and no .NET, Flutter, Xcode, Android SDK, JDK, Docker or `gh`.
- **Render pipeline:** `python3 -I tools/render_mockups.py`.
  1. Syncs the icon-font subset.
  2. Checks every screen for overflow, in light and dark.
  3. Renders `prototype/ui-mock/screens.html?s=<id>&t=<theme>&p=<palette>` to 1170×2532 PNGs (3×).
  4. Builds `deck.html` and prints it to `docs/design/mockups/UI_Mockups_v1.pdf`.

  Flags: `--check-only`, `--deck-only`. Needs an internet connection for Google Fonts.
- **Lessons learned:**
  - Chrome's `--virtual-time-budget` stalls on the full Material Symbols font. Keep the `icon_names=` subset; the script keeps it in sync automatically.
  - Blurred CSS shadows show up as grey boxes in PDF viewers, so the deck uses borders only.
  - Watch for class-name clashes between page CSS and component CSS (`.sec`).
  - Use `.fab > span`, not `.fab span`.
- **Version facts:**
  - .NET 8 and .NET 9 reached end of support on 10 Nov 2026; .NET 10 LTS runs to Nov 2028.
  - Latest confirmed Flutter stable is 3.47 (Aug 2026).
  - SQL Server on Apple Silicon runs via Docker under Rosetta.

## 11. Sources and resources

| Path | What |
|---|---|
| `docs/source/requirements-v1-revised.pdf` | Original 3-page scope (partly superseded) |
| `docs/source/client-clarifications-2026-11-18.md` | Client's written answers, verbatim |
| `docs/source/staff-ui-mockup.pdf` | Client's rough 31-screen staff mock-up (workflow reference only) |
| `docs/source/palette/` | Instagram palette images (superseded) |
| `OPEN_QUESTIONS.md` | Every question round and answer (Rounds 1–1e, owner changes) |
| `docs/ux/wireframes.html` | Greyscale low-fi wireframes (Phase 2, structure only) |
| `docs/design/ui-preview.html` | Rejected "Fleet Signage" preview (history) |
| `prototype/ui-mock/` | The dummy app (`screens.html`, `styles.css` = tokens, `app.js`, `deck.css`) |
| `docs/design/mockups/` | Current deliverable: PNGs (light/dark/alt) + `UI_Mockups_v1.pdf` |
| `2026-11-26-…vehicleandpar.txt` (repo root) | The owner's export of the full working session (raw history), up to 2026-11-26 21:56. Later work (knowledge base rewrite, GitHub push, completeness audit) is reflected in the files themselves |
| `tools/check_contrast.py` | Re-checks every Emerald Ops colour pair against WCAG (26/26 pass) |

**Not in the repo (by design):**
- The assistant's private memory notes (preferences, repo link). Their content is mirrored in §13 and below.
- Generated render copies (`prototype/ui-mock/renders/`, `deck.html`); the render script rebuilds them.
- macOS `.DS_Store` files.

**Repository:** https://github.com/53huzefa-zeenwala/VehicalManagmentSyatemDM, branch `main`. It is **public** by the owner's choice (2026-11-26), so the client documents in `docs/source/` are publicly visible. Commit and push only when the owner asks.

**External references:**
- .NET support: https://learn.microsoft.com/dotnet/core/releases-and-support
- Flutter: https://docs.flutter.dev/install/archive
- WCAG 2.2: https://www.w3.org/TR/WCAG22/
- Fonts and icons: https://fonts.google.com

## 12. Timeline

| Date | What happened |
|---|---|
| 2026-11-15 | Read the PDF and palette; environment check; questionnaire round 1; `CLAUDE.md` and the decision log created |
| 2026-11-18 | Staff UI mock-up analysed; client clarifications received (D-006); owner answers (D-007 one app, D-008 .NET 8, D-009) |
| 2026-11-22 | Rounds 1d/1e answered (D-010–D-012); `REQUIREMENTS.md` v1.0 approved (D-013); UX architecture + wireframes; "Fleet Signage" rejected; "Emerald Ops" dummy app + PDF (D-017, D-018) |
| 2026-11-23 | Staff = driver, admins monitor only, Profile tab (D-020–D-022); attendance removed (D-023); PDF now 24 screens |
| 2026-11-26 | Knowledge base rewritten to the current state; project put under git and pushed to GitHub. Completeness audit: `UX_ARCHITECTURE.md` rewritten to v0.2, `REQUIREMENTS.md` v1.1 updated inline (no stale driver, attendance or admin-end lines left), contrast checker added |
| 2026-11-26 (later) | Development moved to Windows 11 (D-025). Stuck trips stay open (D-024, OI-10 closed). Transfer driving counts as the active trip (D-026). Stale "ended by admin" lines removed from `REQUIREMENTS.md` BR-10 and AUD-01 |
| 2026-11-26 (later) | API development approved to start (D-027). Stack fixed to the Sufizz Bikes backend's SDK and template (D-028). DB connection test: reference connection-string host not reachable; the app server's SQL Server (2019 Express) reachable and login works (OI-12). No code written |
| 2026-11-26 (later) | Owner set API rules: new DB (D-029), migrations at startup (D-030), cookie auth (D-031), one JSON error format + log everything (D-032). `ARCHITECTURE.md` v0.1 written (D-033, proposed). No code written |
| 2026-11-26 (later) | Owner answered the architecture open points: name `VehicleManagementDM`, generic Production errors + traceId, 30-day log retention, files on server disk (D-034), manual refresh (D-035), self-signed cert accepted (D-036, risk accepted), admin IDs `ADM`+YY+5 (D-037). `ARCHITECTURE.md` v0.2 approved. No code written |
| 2026-11-26 (later) | API plan approved (D-042, milestones M0–M6; photos 20 MB). Dev/test DB on server (D-038), in-app notifications first (D-039), QR PDF in app (D-040), hand-over without receiver action (D-041). M0 done: `DATABASE_DESIGN.md` v0.1, `API_DOCUMENTATION.md` v0.1, `ARCHITECTURE.md` v0.3. No code written |
| 2026-11-26 (later) | Owner accepted all DB-design answers (D-043): `DATABASE_DESIGN.md` v0.2 and `API_DOCUMENTATION.md` v0.2 approved; M0 closed |
| 2026-11-27 | **M1 built** in `backend/`: solution, errors, logging, migrations, cookie auth, file store, auth/files/health endpoints; 49 tests pass against the server; `VehicleManagementDM_Dev` created on the server. D-044 proposed. `CHANGELOG.md` started |
| 2026-11-27 (later) | Owner: "complete all remaining work". **M4** transfers, **M5** notifications/reminder job/dashboard/reports/audit log, vehicle history, **M6** publish profile, `web.config`, `DEPLOYMENT.md`; migration `TransfersAndNotifications` applied to the dev database; 105 tests pass; D-048 proposed |
| 2026-11-27 (later) | Owner approved D-048. No open decisions remain for the API |
| 2026-11-27 (later) | Owner: controllers into one folder. All 13 controllers moved to `Controllers/` (D-049); 105 tests pass |
| 2026-11-27 (later) | Owner: IIS, runtime 8.0.18, port 8445 (OI-13 resolved, D-050). Release package built; changes committed and pushed |
| 2026-11-27 (later) | Owner: store passwords in plain text (D-051, risk explained and accepted). Migration `PlainTextPasswords` applied to the dev database (existing dev accounts need a new password); first-admin SQL script in DEPLOYMENT.md §3.4; 106 tests pass |
| 2026-11-27 (later) | Owner: production uses `sa` (D-052, OI-12 resolved, risk explained). Server settings in `appsettings.Production.json` (git-ignored, never published); template prepared locally, password filled in by the owner before deploy |
| 2026-11-27 (later) | **First deploy.** Owner set up the IIS site on port 8445, data folders and `appsettings.Production.json`. API created the database and applied all 6 migrations on first start (503 `DATABASE_NOT_READY` until done). Checked from outside: `/health` Healthy, unknown route JSON 404, no session JSON 401, bad login `INVALID_CREDENTIALS`, Swagger off. Next: first admin (DEPLOYMENT.md §3.4), restart check, backups |
| 2026-11-27 (later) | Owner created the first admin `ADM2600001` on the server with the SQL script. Added `tools/seed-test-data.ps1` (TEST master data through the API, idempotent; tested twice on a throwaway local database). Owner runs it against the server, since Claude does not type passwords into the live server |
| 2026-11-27 (later) | Owner's seed run got `503 DATABASE_NOT_READY`: IIS had stopped the idle app and the first request restarted it. Fix: requests wait up to 15 s for startup (`Database:StartupWaitSeconds`); DEPLOYMENT.md asks for Idle Time-out 0 + Preload Enabled. 107 tests pass; release zip rebuilt, not yet deployed |
| 2026-11-27 (later) | Owner's 2nd seed run also got 503 (server still on the old build). `seed-test-data.ps1` now retries sign-in on `DATABASE_NOT_READY` (12 × 5 s; tested against an unreachable database). **Security finding:** the SQL Server error log shows continuous `sa` password-guessing from many internet IPs (hundreds to 1,000+ attempts per IP, 2026-11-20 to 27). Port 1433 must be firewalled and the `sa` password changed (D-052 risk) |
| 2026-11-27 (later) | Owner changed the `sa` password and limited port 1433 to the dev PC's IP (AWS security group + Windows Firewall rule *SQL 1433 - trusted IPs only*); no outside login attempts since. Dev/test user-secrets updated; 107 tests pass. Seed script run on the server: 2 categories / 4 types, lots TEST-A/TEST-B, 2 areas / 4 locations, vehicles TEST-1001…1005, staff DM2600001/DM2600002; all audited. Verified via SQL |
| 2026-11-27 (later) | Owner: two apps (staff, admin), staff first, first cut; one repo with shared core; Android only; Emerald Ops approved; toolchain = owner's working setup. D-053–D-055 logged, D-007 superseded, D-014/D-017 approved. Built `app/` workspace (`dm_core` + `apps/staff`) with all staff screens; 17 tests pass, analyzer clean, release APK (66 MB) builds. Not yet checked on a phone |
| 2026-11-28 | **Admin app built** (D-057): all admin screens; shared screens moved into `dm_core`. Owner removed the map and lot coordinates from the whole system (D-056; migration `RemoveLotCoordinates`). 33 Flutter tests + 107 backend tests pass; admin API calls checked against a local API |
| 2026-11-27 (later) | Phone test (Nothing Phone 3a, Android 17, over Tailscale + wireless adb, local API with a throwaway database): all staff flows work. 7 bugs found and fixed (icons → built-in rounded set, grey screen after ending, false "session ended", amber controls, scanner layout, handed-over trips missing in My trips, transfer driving-time row). Error capture added. 19 tests pass. Throwaway database dropped. Next: live-server check with the owner signing in |
| 2026-11-27 (later) | Owner approved M3 and D-046, then removed staff lot assignment (D-047): API, migration `RemoveStaffLots`, tests (94 pass) and docs updated |
| 2026-11-27 (later) | Owner approved M2 and D-045. **M3 built**: trips with app-lock concurrency, `Trips` migration applied to the dev database; 95 tests pass. D-046 proposed |
| 2026-11-27 (later) | **M2 built**: master data endpoints, occupancy, audit interceptor, `MasterData` migration applied to the dev database; 78 tests pass. D-045 proposed |
| 2026-11-27 (later) | Owner changed D-044: unknown routes answer 404 to signed-out callers too (was 401); password rule kept. D-044 approved; 51 tests pass |

## 13. Owner preferences

- Reports must be **extremely concise**.
- Ask **one** clarifying question when a request is ambiguous.
- Production quality.
- Prefers **forms with selectable options** for questions, in short rounds.
- Wants **realistic, good-looking UI shown as a PDF of rendered screens**, not greyscale wireframes or HTML preview pages.
- Takes part in every important decision; every decision is logged.
- Makes product changes directly ("driver is staff", "remove attendance"). Record each one as a decision and propagate it to every doc and screen.
