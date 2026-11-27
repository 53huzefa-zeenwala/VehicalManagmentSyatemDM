# Project Decisions Log

Permanent record of every important decision. Never delete entries: replaced decisions are marked `Superseded` with a link to the replacement.

**Status values:** Proposed · Approved · Rejected · Superseded
**Categories:** Business logic · UX/UI · Architecture · Technology · Database · API · Security · Deployment · Performance · Process

## Index

| ID | Date | Category | Decision | Status |
|---|---|---|---|---|
| D-001 | 2026-11-15 | Business logic | Two dedicated mobile apps + one centralized backend; no web portal | Superseded by D-007 |
| D-002 | 2026-11-15 | Technology | Flutter · ASP.NET Core Web API · SQL Server | Approved (version → D-008) |
| D-003 | 2026-11-15 | Technology | Backend runtime: .NET 10 LTS instead of .NET 8 | Rejected (→ D-008) |
| D-004 | 2026-11-15 | UX/UI | Palette: Ink Heather / Putty Grey / Canary Flash, 60/30/10 | Superseded by D-017 |
| D-005 | 2026-11-15 | Process | UX-first phased delivery; no code before requirements are clarified | Approved |
| D-006 | 2026-11-18 | Business logic | Client clarifications (batch 1) adopted as requirements | Approved (source: client) |
| D-007 | 2026-11-18 | Architecture | One mobile app; the role (Admin or Staff) comes from the account | Superseded by D-053 |
| D-008 | 2026-11-18 | Technology | Keep .NET 8 for the API (end-of-life risk accepted) | Approved |
| D-009 | 2026-11-18 | Business logic | Business rules batch 2 (owner answers, form 3) | Approved; driver and admin-end parts superseded by D-020, D-021 |
| D-010 | 2026-11-22 | Business logic | Business rules batch 3 (owner answers, form 4) | Approved; driver parts superseded by D-020 |
| D-011 | 2026-11-22 | Business logic | Staff ID format `DM` + YY + 5-digit sequence | Approved 2026-11-22 |
| D-012 | 2026-11-22 | Business logic | Business rules batch 4 (form 5 + final questions) | Approved; Driver ID part superseded by D-020 |
| D-013 | 2026-11-22 | Process | `REQUIREMENTS.md` v1.0 approved, including P-01 to P-22; Phase 1 closed | Approved; P-02, P-11 superseded (D-020, D-021) → v1.1 |
| D-014 | 2026-11-22 | UX/UI | UX structure: role-based navigation, active-trip bar, flows (UX-01 to UX-11) | Approved (2026-11-27, D-054) |
| D-015 | 2026-11-22 | UX/UI | Design system v0.1 "Fleet Signage": tokens, fonts, icons, components | Rejected |
| D-016 | 2026-11-22 | Process | Review the UX structure and design system together, on themed screens | Approved |
| D-017 | 2026-11-22 | UX/UI | New visual direction "Emerald Ops" (colours chosen by Claude, as the owner delegated) | Approved (2026-11-27, D-054) |
| D-018 | 2026-11-22 | Process | Design deliverables = dummy app → screenshots → PDF; project knowledge base | Approved |
| D-019 | 2026-11-22 | UX/UI | Centre Scan button in both tab bars (UX-12) | Superseded by D-022 |
| D-020 | 2026-11-23 | Business logic | The staff member is the driver; no separate driver records | Approved |
| D-021 | 2026-11-23 | Business logic | Admins monitor only: no ending, swapping, cancelling or scanning | Approved |
| D-022 | 2026-11-23 | UX/UI | Tab bars: Staff = Home · Trips · Scan · Lots · Profile; Admin = 4 tabs, no Scan | Approved |
| D-023 | 2026-11-23 | Business logic | Attendance removed from the app (v1 scope) | Approved |
| D-024 | 2026-11-26 | Business logic | Stuck trips (staff can't act) stay open; risk OI-10 accepted | Approved |
| D-025 | 2026-11-26 | Process | Development continues on a Windows 11 machine (project started on a Mac) | Approved |
| D-026 | 2026-11-26 | Business logic | Driving a transfer counts as the staff member's one active trip | Approved |
| D-027 | 2026-11-26 | Process | Start backend (API) development now, ahead of Phases 4–6 (owner instruction) | Approved |
| D-028 | 2026-11-26 | Technology | API uses the same SDK, target framework and project template as the existing Sufizz Bikes backend; nothing new installed on the server | Approved |
| D-029 | 2026-11-26 | Database | A completely new, empty database for this app | Approved |
| D-030 | 2026-11-26 | Database | EF Core code-first migrations; API applies them at startup and creates the DB if missing | Approved |
| D-031 | 2026-11-26 | Security | Cookie-based authentication for the API | Approved |
| D-032 | 2026-11-26 | API | Every error logged and returned in one JSON format, never HTML; real error sent where possible | Approved (format details proposed in D-033) |
| D-033 | 2026-11-26 | Architecture | API structure and conventions: `ARCHITECTURE.md` (v0.1, answers in v0.2) | Approved 2026-11-26 |
| D-034 | 2026-11-26 | Architecture | Photos and signatures stored in a server folder; DB keeps only the API-generated unique file name | Approved |
| D-035 | 2026-11-26 | Business logic | No live auto-updates: users refresh manually | Approved |
| D-036 | 2026-11-26 | Security | Self-signed HTTPS certificate; the app accepts every certificate (risk accepted) | Approved |
| D-037 | 2026-11-26 | Business logic | Admin ID format `ADM` + YY + 5 digits | Approved |
| D-038 | 2026-11-26 | Process | Dev and test databases on the server's SQL Server (separate from production) | Approved |
| D-039 | 2026-11-26 | Architecture | Notifications: in-app list now; push (FCM) plugged in later | Approved |
| D-040 | 2026-11-26 | Architecture | QR images and PDF labels generated in the Flutter app; API returns label data | Approved |
| D-041 | 2026-11-26 | Business logic | Hand-over needs no action from the receiving staff member | Approved |
| D-042 | 2026-11-26 | Architecture | API implementation plan (milestones M0–M6); photo uploads up to 20 MB | Approved |
| D-043 | 2026-11-26 | Database | Data rules Q1–Q4 and `DATABASE_DESIGN.md` v0.2 approved; M0 closed | Approved |
| D-044 | 2026-11-27 | Security | Password length 8–128 characters; unknown API routes answer 404 to everyone | Approved (route part changed by owner) |
| D-045 | 2026-11-27 | Business logic | Settings limits (prefix 1–5 letters, reminder delay 1–1,440 min) and M2 scope notes | Approved |
| D-046 | 2026-11-27 | Business logic | Trip details settled while building M3 (origin = vehicle's lot, swap from any active lot, overdue rule, visibility) | Approved |
| D-047 | 2026-11-27 | Business logic | Staff have no assigned lots: any staff member (the driver) can start from any active lot and confirm any transfer | Approved |
| D-048 | 2026-11-27 | Business logic | Details settled while building M4–M6 (transfer rules, notification audiences, dashboard and report definitions, deployment layout) | Approved |
| D-049 | 2026-11-27 | Architecture | API controllers live in one `Controllers/` folder; business logic stays in `Features/<Area>` | Approved |
| D-050 | 2026-11-27 | Deployment | API hosted in IIS on the app server, port 8445 (HTTPS), ASP.NET Core 8.0.18 runtime; first admin created by the owner | Approved |
| D-051 | 2026-11-27 | Security | Passwords stored as plain text (replaces PBKDF2 hashing); risk accepted by the owner | Approved |
| D-052 | 2026-11-27 | Deployment | Production connects with the `sa` login; server settings in `appsettings.Production.json` kept only on the server; risk accepted by the owner | Approved |
| D-053 | 2026-11-27 | Architecture | Two mobile apps (Staff, Admin) sharing one core package; replaces D-007 | Approved |
| D-054 | 2026-11-27 | Process | Start the Flutter phase now, before all Phase 4 screens exist; D-014 and D-017 approved | Approved |
| D-055 | 2026-11-27 | Technology | Mobile stack and toolchain pins (Flutter 3.35.3, Android SDK 36, AGP 8.7.2, Riverpod, go_router, dio) | Approved |

---

## D-001 — Product scope: two mobile apps, one centralized backend

- **Date:** 2026-11-15
- **Category:** Business logic
- **Decision:** Build an Admin App and a Staff App, each for Android and iOS, served by one centralized backend, API and database. A web Admin Portal, Staff Portal or website is out of scope.
- **Reasoning:** Stated in requirements PDF §1 (Project Deliverables, Scope note) and §4.
- **Alternatives considered:** None. Set by the source document.
- **Final selection:** As stated.
- **Status:** Superseded by D-007 on 2026-11-18: one app instead of two. The centralized backend and the no-web-portal parts still stand. (Originally approved from `docs/source/requirements-v1-revised.pdf`.)
- **Impact:** All admin functions, including reports and QR downloads, must work well on a phone. How the two apps share code is an architecture decision for Phase 5.

## D-002 — Technology stack

- **Date:** 2026-11-15
- **Category:** Technology
- **Decision:** Flutter for both mobile apps, ASP.NET Core Web API for the backend, Microsoft SQL Server for the database.
- **Reasoning:** Specified by the project owner. No compatibility concern with the requirements found so far.
- **Alternatives considered:** None evaluated. The stack was given as intended.
- **Final selection:** As stated. The runtime version is decided in D-008 (D-003 rejected).
- **Status:** Approved (user-specified)
- **Impact:** Requires the full toolchain on the dev machine (see `PROJECT_OVERVIEW.md` §9).

## D-003 — Backend runtime: .NET 10 LTS instead of .NET 8

- **Date:** 2026-11-15
- **Category:** Technology
- **Decision:** Build the API on ASP.NET Core with .NET 10 (LTS).
- **Reasoning:** .NET 8 reached end of support on 10 Nov 2026, so it no longer receives security patches. A new production system should not start on an unsupported runtime. .NET 10 is the current LTS release, supported until Nov 2028. Same framework family: no architectural change.
- **Alternatives considered:** .NET 8 (out of support, rejected on security grounds). .NET 9 (also out of support since 10 Nov 2026).
- **Final selection:** Not adopted. The owner chose to stay on .NET 8 (D-008).
- **Status:** Rejected (2026-11-18)
- **Impact:** SDK installation, CI images, hosting runtime version.

## D-004 — Colour palette basis

- **Date:** 2026-11-15
- **Category:** UX/UI
- **Decision:** Base both themes on Ink Heather (60%, main), Putty Grey (30%, secondary) and Canary Flash (10%, accent, `#FFE014`). Proposed reading: dark theme uses Ink Heather backgrounds with Putty text and secondary elements; light theme uses warm off-white backgrounds, Putty surfaces and borders, and Ink text. Canary is the accent in both themes.
- **Reasoning:** Taken from the reference images. Only Canary has an exact hex value. Ink Heather (≈ `#262A33`) and Putty Grey (≈ `#C1BBAF`) were sampled from compressed screenshots.
- **Alternatives considered:** Using image 2's literal swatches (its "Ink Heather" swatch shows a light beige, which contradicts image 1).
- **Final selection:** Ink Heather `#262A33`, Putty Grey `#C1BBAF` (estimated values accepted), Canary Flash `#FFE014`. Light and dark mapping as described above.
- **Status:** Superseded by D-017 on 2026-11-22. The owner released the palette ("you can choose any color"). Originally approved 2026-11-18.
- **Impact:** Phase 3 design system. Canary cannot be used for text or icons on light backgrounds (contrast 1.24:1). Status colours need a separate semantic set.

## D-005 — Delivery process

- **Date:** 2026-11-15
- **Category:** Process
- **Decision:** Phased delivery: requirements → UX → UI system → hi-fi UI → architecture → database → backend → Flutter → QA → deployment. No code, screen design or architecture until requirements are clarified.
- **Reasoning:** Instruction from the project owner.
- **Alternatives considered:** None.
- **Final selection:** As stated.
- **Status:** Approved
- **Impact:** Governs the order of all work.

## D-006 — Client clarifications (batch 1) adopted as requirements

- **Date:** 2026-11-18
- **Category:** Business logic
- **Decision:** Adopt the client's written clarifications (`docs/source/client-clarifications-2026-11-18.md`) as requirements:
  - Staff access is limited to: own profile, the vehicles they started, lot details, availability across all lots, start and end.
  - Start means the vehicle leaves the lot; End means it returns; duration is the time spent outside the lot.
  - Staff end their own sessions. The admin sees active vehicles in real time and can end any session.
  - Unended sessions trigger repeated reminders, and a live timer shows in the Staff app's top bar.
  - Start and End record: staff name and ID, driver name, vehicle registration, category, brand and model, origin lot, and start and end date and time with duration.
  - Lots have: type (Open, Underground, Other), total and per-vehicle-type capacity, auto-calculated occupancy, Active/Inactive status, and the full detail list (name, code, address, Google Maps pin, contact, operating hours). Admins get a Map View of all lots.
  - Vehicles have: registration number, type, brand, model, colour, photo.
  - Login: one dedicated admin account with full access, credentials set up by our team. Staff get unique Staff IDs and passwords issued by the admin.
- **Reasoning:** Stated directly by the client.
- **Alternatives considered:** Mockup-only features (attendance, modify, cancel, signature, destinations) are pending as K6.
- **Final selection:** As stated.
- **Status:** Approved (source: client)
- **Impact:**
  - New integrations: push notifications (reminders), Google Maps (pin and Map View), real-time updates for the admin's active-vehicle view.
  - Answers A1, B1, B3, C1, C3, C4, C5, D1, D3, E1, F1 and N9 (see `OPEN_QUESTIONS.md`).

## D-007 — One mobile app with role-based access

- **Date:** 2026-11-18
- **Category:** Architecture
- **Decision:** One Flutter app for Android and iOS. After login, the role assigned on the server (Admin or Staff) decides which features appear. The login screen has no role toggle.
- **Reasoning:** Owner's choice (N1). One store listing, one codebase, simpler distribution and updates.
- **Alternatives considered:** Two apps sharing one codebase, as listed in the original PDF scope.
- **Final selection:** One app.
- **Status:** Superseded by D-053 on 2026-11-27 (two apps sharing one core package). Originally approved; it superseded the "two dedicated apps" part of D-001.
- **Impact:**
  - The PDF's deliverables list says two apps, so the client should be told.
  - Admin features ship inside the app on staff's personal phones, hidden by role. The API must check every permission on the server, never trust the app.

## D-008 — Keep .NET 8 for the API

- **Date:** 2026-11-18
- **Category:** Technology
- **Decision:** Build the API on ASP.NET Core with .NET 8.
- **Reasoning:** Owner's choice (H1).
- **Alternatives considered:** .NET 10 LTS (D-003, rejected).
- **Final selection:** .NET 8.
- **Status:** Approved. Risk accepted by the owner.
- **Impact:** .NET 8 support ended on 10 Nov 2026, so Microsoft no longer ships security patches for it. Moving from 8 to 10 is usually a small change. Revisit before Phase 7 (backend) or before going live.

## D-009 — Business rules batch 2 (owner answers, form 3)

- **Date:** 2026-11-18
- **Category:** Business logic
- **Decision:**
  - **Drivers:** admins manage the driver list; staff choose the driver at Start (whether drivers can be scanned is still open: N2b).
  - **Hours:** reported both per staff member and per driver.
  - **Concurrency:** a staff member can have only one active vehicle at a time. A driver and a vehicle can each be in only one active trip (this follows logically).
  - **Lots for staff:** staff can start vehicles only from their assigned lots. At End, staff choose the lot the vehicle returned to, which can be any lot. *(Assigned lots superseded by D-047.)*
  - **Reminders:** the first one goes out after a duration the admin sets, then every 30 minutes, and the admin is alerted too.
  - **Admin manual end:** the admin enters the actual end time and a required reason, and the change is recorded in the audit log.
  - **Capacity:** available spaces are calculated automatically, and the admin can set a starting count. A full lot or vehicle type gets a warning but is still allowed. Operating hours are information only.
  - **Vehicle statuses:** Available, Active (out of the lot), Inactive, Under maintenance.
  - **Transfers:** the admin starts the transfer and staff confirm the vehicle arrived.
  - **Categories:** each vehicle Type belongs to a Category. Capacity per type uses the Type level (the client's wording "Vehicle Type-wise Capacity").
  - **QR codes:** the code carries no readable data. Admins can download an image or a printable PDF label, and bulk print. Staff can type the ID if a scan fails. Admins can also scan. A damaged sticker is reprinted with the same code (my reading, since "reissue cancels old" wasn't chosen).
  - **Accounts:** the system generates Staff IDs in a fixed pattern (pattern still open: K4). More admin accounts can be added. The admin resets staff passwords and our team resets the admin's. No forced password change at first login.
  - **Devices:** staff use their personal phones.
  - **Removal:** records are deactivated, never deleted, so history is kept.
  - **Connectivity:** online only.
  - **Tenancy:** a single organisation.
  - **Out of scope for v1:** billing and payments, maintenance management, fuel logs, live vehicle GPS.
  - **Kept from the mockup:** attendance, modify trip, cancel trip with reason, driver signature, destination location.
  - **Dropped from the mockup:** driver QR cards.
- **Reasoning:** Owner and client answers given through form 3.
- **Alternatives considered:** Listed in `OPEN_QUESTIONS.md` for each question.
- **Final selection:** As stated.
- **Status:** Approved
- **Impact:**
  - Push notifications (FCM/APNs) and a scheduled background job are needed for reminders.
  - Personal phones plus attendance photos, GPS and signatures mean personal data handling and a privacy policy.
  - "Under maintenance" is set by hand; there is no maintenance module.
  - Follow-up questions are listed under "Round 1d" in `OPEN_QUESTIONS.md`.

## D-010 — Business rules batch 3 (owner answers, form 4)

- **Date:** 2026-11-22
- **Category:** Business logic
- **Decision:**
  - **Driver selection:** staff pick the driver from a list or type the Driver ID. There are no driver QR cards. This resolves the open N2b point in D-009.
  - **Driver fields:** photo, Driver ID, full name, mobile, licence number.
  - **Driver eligibility:** none; any driver can take any vehicle type.
  - **Attendance:** staff only, on their own phones. Events are check in, check out, lunch break and tea break. A photo and a GPS location are mandatory for every entry.
  - **Destinations:** a list the admin manages, structured as Area → Location.
  - **Modifying a trip:** swapping the vehicle or driver edits the same trip; no new trip is created.
  - **Cancelling a trip:** the trip ends early and its hours still count. The vehicle's status is not changed automatically; the admin decides.
  - **Driver signature:** taken at Start only.
  - **Ending at another lot:** staff can end a trip at any lot, including lots they aren't assigned to.
  - **Transfers:** the vehicle stays counted at the origin lot until staff confirm it arrived. A transfer records a driver and counts toward operating hours.
- **Reasoning:** Owner answers through form 4.
- **Alternatives considered:** Listed in `OPEN_QUESTIONS.md` for each question.
- **Final selection:** As stated.
- **Status:** Approved
- **Impact:** Swapping drivers or vehicles inside one trip conflicts with reporting hours per driver and per vehicle (N3, running-hours reports). The proposed fix is to log each swap as a timed segment (Round 1e, P1).

## D-011 — Staff ID format (proposed)

- **Date:** 2026-11-22
- **Category:** Business logic
- **Decision:** Staff IDs are `DM` + 2-digit year the account was created + 5-digit sequence, for example `DM2600042`.
  - The prefix is an admin setting.
  - Sequence numbers are never reused, including after a staff member is deactivated.
  - Login ignores letter case.
- **Reasoning:**
  - The ID never changes, and it appears in history and audit records.
  - Lots and areas are left out of the ID because staff assignments change.
  - Digits after the prefix avoid O/0 and I/1 mix-ups when typing on a phone.
  - Nine characters is short enough to type at login.
- **Alternatives considered:**
  - Encode the lot or area, as the client's example `DM324DU2131` might. Rejected: the ID would go stale when assignments change.
  - Add a check digit. Not needed.
- **Final selection:** As proposed.
- **Status:** Approved (2026-11-22)
- **Impact:** Login, Staff Management and audit display.

## D-012 — Business rules batch 4 (form 5 + final questions)

- **Date:** 2026-11-22
- **Category:** Business logic, UX/UI
- **Decision:**
  - **Hours after a swap:** a trip stays one record, but every driver or vehicle swap is logged as a timed segment. Hours are split by segment, per driver and per vehicle.
  - **Driver ID:** the driver's existing employee number, entered by the admin. It must be unique.
  - **Vehicle location after cancel or swap-out:** staff pick the lot where the vehicle is, or mark it "Not at a lot". In that case the admin sets its lot and status later.
  - **Transfer hours:** start when staff at the origin lot confirm departure, and end when staff confirm arrival. The vehicle stays counted at the origin lot until arrival (D-010).
  - **Attendance rules:** the admin sets the shift start time, working days and a grace period. Late = check-in after shift start plus grace. Absent = no check-in on a working day.
  - **Breaks:** lunch and tea breaks each record a start and an end.
  - **Language:** English in v1. Every layout is built to work right-to-left, so Arabic can be added later without a redesign.
  - **Reports:** viewed on screen with date and other filters. No PDF or Excel export in v1.
- **Reasoning:** Owner answers on 2026-11-22.
- **Alternatives considered:** Listed in `OPEN_QUESTIONS.md`.
- **Final selection:** As stated.
- **Status:** Approved
- **Impact:**
  - Trip data needs a segment table.
  - The design system must work in both left-to-right and right-to-left layouts (directional icons, start/end spacing).
  - No file generation is needed for reports in v1.

## D-013 — Requirements v1.0 approved

- **Date:** 2026-11-22
- **Category:** Process
- **Decision:** Approve `REQUIREMENTS.md` v1.0 as the functional baseline, including all of my proposals P-01 to P-22 (§11.1). Phase 1 is closed and Phase 2 (UX) starts.
- **Reasoning:** Owner approval ("Approve all, start UX").
- **Alternatives considered:** Reviewing each proposal one by one (the owner declined).
- **Final selection:** Approved as written.
- **Status:** Approved
- **Impact:** UX work builds on `REQUIREMENTS.md`. Any later change to requirements needs a new decision entry.

## D-014 — UX structure

- **Date:** 2026-11-22
- **Category:** UX/UI
- **Decision:** Adopt the UX decisions UX-01 to UX-11 in `docs/ux/UX_ARCHITECTURE.md` §10:
  - **Navigation:** bottom navigation by role. Staff: Home · Trips · Lots · Attendance. Admin: Dashboard · Live · Manage · Reports.
  - **Active-trip bar:** pinned on every staff screen while a trip runs.
  - **Start trip:** a stepped flow ending in a review screen.
  - **End trip:** scan the vehicle to confirm it, with the origin lot pre-selected as the return lot.
  - **Confirm sheets:** for End, Cancel and admin End.
  - **Live tab (admin):** trips and transfers in one place.
  - **Manage hub:** grouped into Fleet, People, Places and System.
  - **Driver picker:** recent drivers first.
  - **Attendance:** one next-action button plus a timeline.
  - **Dashboard:** "Needs attention" first.
  - **Permissions:** asked just in time, not at first launch.
- **Reasoning:**
  - Staff work in the field and need speed and very few mistakes.
  - Admins manage by exception.
  - Both follow platform conventions (3–5 bottom tabs) and the client's requirements (always-visible timer, real-time active list).
- **Alternatives considered:**
  - One home screen of tiles, as in the mockup. Rejected: deeper navigation and no persistent context.
  - A drawer menu for admins. Rejected: it hides the main areas.
  - A smart "scan to start or end" button. Deferred: unclear to new users.
- **Final selection:** Pending owner approval.
- **Status:** Approved by the owner on 2026-11-27 ("Use Emerald Ops", D-054). Staff screens follow it; mock-up items changed by later decisions (D-047, D-043, D-046) follow those decisions.
- **Impact:** Phase 3 components (timer bar, step flow, confirm sheet, pickers) and Phase 4 screens. Wireframes: `docs/ux/wireframes.html`.

## D-015 — Design system v0.1 "Fleet Signage" (proposed)

- **Date:** 2026-11-22
- **Category:** UX/UI
- **Decision:** Adopt `docs/design/DESIGN_SYSTEM.md` v0.1.
  - **Colour:** semantic colour tokens for light and dark, all contrast-checked.
  - **Canary:** used only for things that are live and for primary actions.
  - **Status colours:** muted tints, always shown with an icon and text. Warning is orange, never canary.
  - **Fonts:** Readex Pro for display, IBM Plex Sans for body, IBM Plex Mono for timers, plates and IDs. Readex Pro and Plex Sans both have Arabic versions for the right-to-left phase.
  - **Icons:** Material Symbols Rounded.
  - **Layout:** 4-pt spacing grid; corner radii 6, 10, 14 and 22.
  - **Signature components:** the canary live strip and plate badges.
- **Reasoning:**
  - It applies the approved palette (D-004) in a calm enterprise style, as the owner asked (no decorative KPI tiles).
  - It is built for outdoor readability.
  - It is ready for Arabic.
  - Every pairing passes WCAG AA, as computed.
- **Alternatives considered:**
  - The generic blue palette and Fira fonts the design tool suggested. Rejected: they conflict with D-004.
  - Inter or Roboto. Rejected: generic, and no matching Arabic family.
  - Canary as text on light backgrounds. Rejected: 1.19:1 contrast.
- **Final selection:** Not adopted.
- **Status:** Rejected (2026-11-22). Owner: "theme need a huge improvement… not what I want; I want good looking UI". Replaced by D-017.
- **Impact:** Every Phase 4 screen and the Flutter theme (Phase 8: `ThemeExtension` + 2 `ThemeData`). The fonts must be bundled as app assets.

## D-016 — Review the UX structure and design system together

- **Date:** 2026-11-22
- **Category:** Process
- **Decision:** At the owner's request ("I want design with theme"), review the UX structure (D-014) and the design system (D-015) together on themed key screens (`docs/design/ui-preview.html`), instead of approving the greyscale wireframes first.
- **Reasoning:** The owner's instruction. Themed screens make both easier to judge.
- **Alternatives considered:** Strictly following D-005, approving Phase 2 before Phase 3.
- **Final selection:** A combined review.
- **Status:** Approved
- **Impact:** D-005's order still holds for everything else. Phase 4 (all screens, all states) starts only after D-014 and D-015 are approved.

> **2026-11-22 update:** D-015 was rejected by the owner ("not what I want; I want good-looking UI"). D-004 is superseded by D-017.

## D-017 — Visual direction "Emerald Ops"

- **Date:** 2026-11-22
- **Category:** UX/UI
- **Decision:** Replace the Instagram palette (D-004) and the "Fleet Signage" design system (D-015) with "Emerald Ops":
  - **Colour:** emerald primary (`#0B7A62` light / `#34D3A6` dark) and amber `#FFB020` for live states. Night dark mode `#0A1214`.
  - **Font:** Plus Jakarta Sans.
  - **Style:** gradient hero cards, vehicle illustrations, plate badges, the live bar, charts and a map.
  - **Spec:** `docs/design/DESIGN_SYSTEM.md` v1.0.
- **Reasoning:**
  - The owner rejected v0.1 and delegated the colour choice: "you can choose any color".
  - Emerald reads as trustworthy and municipal or sustainable, and gives strong contrast (white on emerald 5.3:1).
  - Amber is a natural "live / in progress" signal.
  - Every text pairing passes WCAG AA (computed).
- **Alternatives considered:** Ocean (blue + orange) and Midnight Lime. Both are rendered on the last page of the PDF; switching is a token change.
- **Final selection:** Emerald Ops, pending the owner's review of `docs/design/mockups/UI_Mockups_v1.pdf`.
- **Status:** Approved by the owner on 2026-11-27 ("Use Emerald Ops", D-054).
- **Impact:**
  - Supersedes D-004.
  - D-015 is rejected.
  - Arabic will need a paired Arabic font later (Plus Jakarta Sans has no Arabic script).

## D-018 — Deliverable format and project knowledge base

- **Date:** 2026-11-22
- **Category:** Process
- **Decision:**
  - **Mock-ups:** UI is shown as a dummy app (`prototype/ui-mock`, HTML/CSS at iPhone size) rendered by headless Chrome to 3× PNGs and compiled into a PDF (`tools/render_mockups.py` → `docs/design/mockups/`). No HTML preview pages for review.
  - **Knowledge base:** all project knowledge and resources are kept in `docs/KNOWLEDGE_BASE.md`.
- **Reasoning:** The owner's instruction ("create dummy app and use screenshots to create PDF… put all knowledge… in files").
- **Alternatives considered:**
  - A Flutter dummy app. Not possible yet: no Flutter, Xcode or Android SDK on this Mac (H2 deferred).
  - Image generation (no image model available).
- **Final selection:** HTML dummy app → PNG → PDF.
- **Status:** Approved
- **Impact:** Rendering needs Chrome and an internet connection (Google Fonts). Re-run the script after any screen change.

## D-019 — Centre Scan button (proposed, UX-12)

- **Date:** 2026-11-22
- **Category:** UX/UI
- **Decision:** Both tab bars get a raised centre **Scan** button.
  - **Staff tab bar:** Home · Trips · Scan · Lots · Attendance. Staff scan to start a trip, end it, or confirm a transfer.
  - **Admin tab bar:** Dashboard · Live · Scan · Manage · Reports. Admins scan to open a vehicle.
- **Reasoning:** Scanning is the most frequent staff action. This puts it one thumb-reach away on every screen. It refines UX-01 (D-014).
- **Alternatives considered:** A scan button only on Home and in the top bar (wireframe v0.1).
- **Final selection:** Replaced by D-022 (Scan button for staff only).
- **Status:** Superseded by D-022 (2026-11-23)
- **Impact:** `UX_ARCHITECTURE.md` §3 and the Phase 8 navigation shell.

## D-020 — The staff member is the driver

- **Date:** 2026-11-23
- **Category:** Business logic
- **Decision:** There is no separate driver; the staff member who starts a trip drives it.
  - **Removed:** driver records, driver management, the driver picker and Driver ID (DRV-01–03; D-009 driver list; D-010 driver fields; D-012 Driver ID).
  - **Staff profile:** gains the driving licence number.
  - **Start flow:** vehicle (scan) → destination → **staff signature** → review. The signature step is kept: the staff member signs to confirm taking the vehicle.
  - **Swap:** change the vehicle, **or hand the trip over to another staff member**. A hand-over starts a new segment and the receiving staff member must have no active trip. Hours are split by segment per staff member and per vehicle (replaces P-11).
  - **Transfers:** no driver is chosen upfront. Any staff member of the origin lot can confirm departure, and whoever confirms it drives the vehicle. Arrival is confirmed by any staff member. *("of the origin lot" superseded by D-047: any staff member.)*
  - **Trip records:** the client's "Driver Name" field equals the staff member, so the summary shows "Driver (staff)".
- **Reasoning:** Owner instruction ("driver is staff") plus answers on 2026-11-23.
- **Alternatives considered:**
  - Removing the signature (rejected by the owner).
  - Allowing vehicle swaps only, or no swaps.
  - Having the admin pick the transfer driver.
- **Final selection:** As stated.
- **Status:** Approved
- **Impact:** `REQUIREMENTS.md` v1.1 §0. Screens: the driver step is removed; Profile, Swap, Review, Summary, My trips and Transfer are changed. Hours reports: "driver hours" equals "staff hours".

## D-021 — Admins monitor only

- **Date:** 2026-11-23
- **Category:** Business logic
- **Decision:**
  - **Trips:** admins do not end, swap or cancel trips. They view live trips, the read-only trip detail and the reminder log, and receive overdue alerts. This supersedes D-009 (admin force-end with time correction), TRP-11, P-02 and the admin part of B4.
  - **Scanning:** admins have no QR scanner (supersedes the admin part of QR-05).
  - **Unchanged:** admins still manage master data, create transfers, set statuses and resolve "Not at a lot" vehicles.
- **Reasoning:** Owner instruction ("admin will not end ride… remove QR scanner") plus the "Monitor only" answer.
- **Alternatives considered:** Letting admins cancel only.
- **Final selection:** Monitor only.
- **Status:** Approved
- **Impact:**
  - The admin-end screen (A-05) is removed and replaced by a read-only trip detail (A-04).
  - **Open risk OI-10:** a trip whose staff member can't act (lost phone, left the job) can't be closed by anyone, so the vehicle stays Active and hours keep growing. This needs a decision before Phase 5.

## D-022 — Tab bar update

- **Date:** 2026-11-23
- **Category:** UX/UI
- **Decision:**
  - **Staff tab bar:** Home · Trips · **Scan** (raised centre) · Lots · **Profile**.
  - **Attendance:** removed from the menu. It opens from the Home attendance card and from Profile.
  - **Admin tab bar:** Dashboard · Live · Manage · Reports, with no Scan button.
- **Reasoning:** Owner instruction on 2026-11-23.
- **Alternatives considered:** The D-019 layout.
- **Final selection:** As stated.
- **Status:** Approved
- **Impact:** UX-01 and UX-12 are updated, and the PDF is re-rendered (`UI_Mockups_v1.pdf`, 25 screens).
- **Note (2026-11-23):** the "attendance opens from Home and Profile" part is superseded by D-023.

## D-023 — Attendance removed from the app

- **Date:** 2026-11-23
- **Category:** Business logic
- **Decision:**
  - **Removed:** staff attendance (check in/out, lunch and tea breaks, photo + GPS, shift rules, Late/Absent) is no longer in the app.
  - **Superseded:** the attendance parts of D-009 (K6), D-010 (N4) and D-012 (P6, P7); ATT-01–08; the shift settings in SET-01; the attendance report in RPT-02.
  - **Screens:** the attendance screen and the attendance cards on Home and Profile are removed. Home now shows the last trip; Profile shows "My trips this month"; the admin dashboard shows "Free spaces" instead of "Staff on duty".
- **Reasoning:** Owner instruction: "attendance page still exists… need to remove".
- **Alternatives considered:** Keeping attendance but hiding it from the menu (the D-022 reading). The owner asked for it to be removed from the screens entirely.
- **Final selection:** Removed from v1.
- **Status:** Approved
- **Impact:**
  - Less personal data: no selfie photos or GPS, so the privacy scope (OI-02) shrinks.
  - The app no longer needs location permission; the camera is now used only for QR scans.
  - The PDF is re-rendered with 24 screens.

## D-024 — Stuck trips stay open (OI-10)

- **Date:** 2026-11-26
- **Category:** Business logic
- **Decision:** A trip whose staff member can't act (lost phone, left the job) stays Active until that staff member ends it. Nobody else can close it; D-021 (admins monitor only) is unchanged.
  - Reminders keep going to the staff member and the admin (TRP-06).
  - The trip stays at the top of the admin's Live tab and in the dashboard's "Needs attention" as overdue.
  - The vehicle stays Active and its hours, and the staff member's hours, keep growing until the trip ends.
- **Reasoning:** Owner's choice ("Leave open").
- **Alternatives considered:**
  - Admin force-close with actual end time and reason (a narrow exception to D-021).
  - Another staff member of the same lot closes it on the original staff member's behalf.
  - Auto-close after an admin-set maximum duration.
- **Final selection:** Leave open; risk accepted.
- **Status:** Approved. Closes OI-10.
- **Impact:**
  - A deactivated staff member's active trip remains Active (AUTH-07 ends the session, not the trip). The vehicle can't be used until the trip ends.
  - Hours reports can include inflated hours for such trips.
  - Recovery outside the app (e.g. a database fix by our team) is not a product feature and would need its own decision.

## D-025 — Development machine: Windows 11

- **Date:** 2026-11-26
- **Category:** Process
- **Decision:** Development continues on a Windows 11 Pro machine. The project was started on an Apple Silicon Mac (environment facts up to 2026-11-26 refer to that Mac).
- **Reasoning:** Owner's statement ("it was created in Mac, now I am continuing development in Windows").
- **Alternatives considered:** Keeping the Mac as the primary machine.
- **Final selection:** Windows 11.
- **Status:** Approved. Partly answers OI-07.
- **Impact:**
  - Found on this machine (2026-11-26): git 2.45, Python 3.12 + Pillow 12.3, Google Chrome, .NET SDK 8.0.202 and 9.0.321 (ASP.NET Core runtime 8.0.3), OpenJDK 21, adb, GitHub CLI, SQL Server Express (`MSSQL$SQLEXPRESS`, stopped). **Not found:** Flutter, Docker, `sqlcmd`.
  - `tools/render_mockups.py` and the README were written for macOS; they need checking on Windows before the next render.
  - iOS builds and signing need a Mac or a cloud macOS build service: new open item OI-11.
  - .NET SDK 8.0.202 is an old patch level; update before Phase 7 (OI-09 still open).

## D-026 — Driving a transfer counts as the one active trip

- **Date:** 2026-11-26
- **Category:** Business logic
- **Decision:** A staff member who confirmed a transfer's departure (and so drives it, D-020) counts as having an active trip until arrival is confirmed.
  - A staff member with an active trip can't confirm a transfer departure.
  - A staff member driving an in-transit transfer can't start a trip or receive a hand-over until the transfer arrives.
- **Reasoning:** Owner's choice. One person can only drive one vehicle at a time; consistent with BR-02 and hours by staff member.
- **Alternatives considered:** Treating transfers and trips as independent.
- **Final selection:** Counts as active.
- **Status:** Approved
- **Impact:** BR-02, TRP-02, TRP-07 (hand-over check) and TRF-02 checks; the transfer-confirm screen (Phase 4) needs a "you have an active trip" blocked state.

## D-027 — Start API development now

- **Date:** 2026-11-26
- **Category:** Process
- **Decision:** Development starts with the backend API. This deviates from the D-005 order: Phase 4 (remaining hi-fi screens), Phase 5 (`ARCHITECTURE.md`) and Phase 6 (`DATABASE_DESIGN.md`) are not done, and D-014 / D-017 are still Proposed.
  - The owner gives the coding instructions and style; the reference project's code and code style are **not** copied.
  - No code is written until the owner says how (2026-11-26: "don't change any code, just update MD files").
- **Reasoning:** Owner instruction ("we will start development, we start with API").
- **Alternatives considered:** Following D-005 strictly (Phase 4 → 5 → 6 first).
- **Final selection:** API first.
- **Status:** Approved
- **Impact:**
  - The API is built from `REQUIREMENTS.md` v1.1 and decisions up to D-028. Data model and architecture choices made during the API work must be logged as decisions and later written into `ARCHITECTURE.md` / `DATABASE_DESIGN.md`.
  - UX changes after D-014 / D-017 review can still change API needs.

## D-028 — API stack matches the existing Sufizz Bikes backend

- **Date:** 2026-11-26
- **Category:** Technology, Deployment
- **Decision:** Build the API on exactly the SDK and project template of the reference project `E:\visual studio project\SufizzBikesBackend\`, because that project is already deployed on the target server and nothing more may be installed there.
  - **Facts read from the reference (2026-11-26):**
    - Project SDK `Microsoft.NET.Sdk.Web`, `TargetFramework` **net8.0**, `Nullable` and `ImplicitUsings` enabled.
    - Visual Studio 2022 solution (format 12.00, VS 17.9), one project.
    - Template: ASP.NET Core Web App (Model-View-Controller): `AddControllersWithViews`, `Views/`, `wwwroot/`, with API controllers mapped by attribute routing.
    - Data access: Entity Framework Core SQL Server provider, code-first migrations (`Migrations/`).
    - Auth: JWT bearer. API docs: Swagger (Swashbuckle).
    - Publish: Visual Studio folder publish (FileSystem), framework-dependent, Release. The server therefore runs the **ASP.NET Core 8 runtime**.
    - Config: `appsettings.json` with `ConnectionStrings` and `Jwt` sections; `launchSettings.json` profiles http / https / IIS Express.
  - NuGet packages ship inside the publish folder, so they don't need installing on the server. Only the runtime (ASP.NET Core 8) is fixed by the server.
- **Reasoning:** Owner instruction. Reuses the server as it is.
- **Alternatives considered:** .NET 10 (D-003, rejected); a separate server.
- **Final selection:** net8.0 + the reference template.
- **Status:** Approved. Confirms D-008 with a hard reason (the server's runtime).
- **Impact:**
  - OI-09 (.NET 8 end of support) stays a risk; upgrading would mean installing the .NET 10 hosting bundle on the shared server.
  - The local SDK 8.0.202 builds net8.0; the server's exact ASP.NET Core 8 patch level is unknown (OI-13).
  - **DB connection check (2026-11-26):** the reference project's production connection string points to a remote SQL Server. From this Windows machine that host is **not reachable** (TCP test failed; SqlClient open timed out after 15 s). The **app server's own SQL Server is reachable** on 1433 and the same login works: SQL Server 2019 Express, 15.0.2000.5 (RTM), login is sysadmin (OI-12).
  - Server addresses, database names and credentials are deliberately **not** written in this repository (it is public).

## D-029 — A new database for this app

- **Date:** 2026-11-26
- **Category:** Database
- **Decision:** The app uses a completely new, empty database of its own on the server's SQL Server. It shares nothing with the Sufizz Bikes database.
- **Reasoning:** Owner instruction ("complete new fresh DB"). Separates data, permissions and backups.
- **Alternatives considered:** Tables inside the existing Sufizz Bikes database.
- **Final selection:** New database.
- **Status:** Approved. Answers part of OI-12.
- **Impact:** Its own SQL login with rights on this database only is proposed in `ARCHITECTURE.md` §3 (not `sa`). SQL Server Express limits it to 10 GB.

## D-030 — Code-first migrations, applied at startup

- **Date:** 2026-11-26
- **Category:** Database
- **Decision:** The database schema comes from the C# model through EF Core migrations. A model change is followed by a new migration; on startup the API applies pending migrations, and creates the database when it doesn't exist.
- **Reasoning:** Owner instruction ("any change on model should change the DB, and at start, if the DB or model doesn't exist, create it").
- **Alternatives considered:** Hand-written SQL scripts; `EnsureCreated` (can't evolve an existing schema, so rejected).
- **Final selection:** Migrations + `Database.Migrate()` at startup.
- **Status:** Approved
- **Impact:**
  - The developer runs `dotnet ef migrations add` after each model change (dev machine only); a test fails if a change has no migration.
  - The app's SQL login needs rights to change the schema of its own database (`db_owner` there), plus `dbcreator` or a hand-created empty database for the very first run.
  - The API must start even when the database is down, otherwise IIS shows an HTML error page (conflicts with D-032). Design in `ARCHITECTURE.md` §3.

## D-031 — Cookie-based authentication

- **Date:** 2026-11-26
- **Category:** Security
- **Decision:** The API authenticates with an encrypted, HttpOnly, Secure cookie (ASP.NET Core cookie authentication), not bearer tokens.
- **Reasoning:** Owner instruction ("I want in API authorization cookie based").
- **Alternatives considered:** JWT bearer access tokens with refresh tokens (the previous `REQUIREMENTS.md` §8 wording).
- **Final selection:** Cookies.
- **Status:** Approved. Supersedes the "short-lived access tokens with refresh tokens" part of `REQUIREMENTS.md` §8 (P-19).
- **Impact:**
  - AUTH-07 (instant deactivation) is met with a per-user security stamp checked on every request.
  - Data Protection keys must persist on the server, or every restart logs all users out.
  - API must answer 401/403 JSON instead of redirecting to a login page.
  - Needs HTTPS (Secure cookie): a domain name with a real certificate before release (OI-14).
  - The Flutter app needs a persistent, secure cookie jar (Phase 8).

## D-032 — Errors: logged, one JSON format, real messages

- **Date:** 2026-11-26
- **Category:** API
- **Decision:**
  - Every error is logged (in the database or a file; the owner has no preference).
  - Every error (database connection, schema, business logic, 500, 401, 403, 404, DTO validation, and so on) returns one JSON format. Never an HTML page or any other format.
  - The real error is sent to the app wherever possible, so the app can show it.
- **Reasoning:** Owner instruction. One format lets the app show errors the same way everywhere.
- **Alternatives considered:** —
- **Final selection:** As stated. The concrete format, error codes, log location and the Production exception-detail policy are proposed in `ARCHITECTURE.md` §5–§6 (D-033).
- **Status:** Approved
- **Impact:** Global exception handler, validation response factory, JSON status-code pages, cookie-event overrides, startup that survives a database outage, an `ErrorLogs` table with a file fallback.

## D-033 — API structure and conventions (proposed)

- **Date:** 2026-11-26
- **Category:** Architecture
- **Decision:** Adopt `ARCHITECTURE.md` v0.1:
  - One API project with feature folders + one test project, under `backend/`.
  - Controllers → services → EF Core `DbContext`; no repository layer, no MediatR/AutoMapper/FluentValidation/Serilog.
  - Error format: RFC 9457 Problem Details + `code` + `errors` + `traceId`; error catalogue in §5.2.
  - Production hides raw exception text for unexpected errors (traceId instead); known errors always send the real message.
  - Error log in the database, file fallback when the database is unreachable; 90-day retention.
  - Conventions: `/api/v1`, camelCase, enums as strings, UTC ISO-8601, paging shape.
- **Reasoning:** Industry practice sized for a small app (owner: "like industry, but not as complicated").
- **Alternatives considered:** Clean Architecture with 4 projects (too much ceremony here); a custom success+error envelope for every response (non-standard); sending raw exception text in Production (security risk).
- **Final selection:** Adopted, with the owner's answers to the open points (2026-11-26):
  - A-1 names: `VehicleManagementDM` (`.sln`, `.Api`, `.Api.Tests`).
  - A-2 Production: "Something went wrong" + traceId for unexpected errors.
  - A-3 error log: database, file fallback; **retention 30 days** (not 90).
  - A-4 → D-034, A-5 → D-035, A-6 → D-036, A-7 → D-037.
- **Status:** Approved 2026-11-26 (proposed earlier the same day).
- **Impact:** `ARCHITECTURE.md` v0.2, §12.

## D-034 — Photos and signatures on the server's disk

- **Date:** 2026-11-26
- **Category:** Architecture
- **Decision:** Vehicle photos, staff photos and signatures are saved as files in a folder on the server and served from there. The database stores only the file name, which the API generates as a unique name (GUID + extension).
- **Reasoning:** Owner instruction. Keeps the database small (SQL Server Express 10 GB limit).
- **Alternatives considered:** Storing images in the database (`varbinary`).
- **Final selection:** Server folder + unique name in the database.
- **Status:** Approved
- **Impact:** Design in `ARCHITECTURE.md` §11:
  - The folder lives outside the publish folder.
  - Files are served only through an authenticated API endpoint, never as public static files, because they are personal data.
  - Type and size are validated.
  - The folder must be backed up along with the database (Phase 10).

## D-035 — No live auto-updates; manual refresh

- **Date:** 2026-11-26
- **Category:** Business logic, UX/UI
- **Decision:** Admin screens (Live tab, dashboard) and all other lists don't update by themselves. Data loads when a screen opens and when the user refreshes manually (pull to refresh). Trip timers still tick on the phone from the server's start time.
- **Reasoning:** Owner instruction ("no need for refresh; the user refreshes manually").
- **Alternatives considered:** Polling every 5 s; SignalR push.
- **Final selection:** Manual refresh.
- **Status:** Approved
- **Impact:**
  - Supersedes the `REQUIREMENTS.md` §8 "Real-time: updates within 5 s" target. TRP-05 "live" now means elapsed time that ticks on the phone, with the list as fresh as the last refresh.
  - The client's clarification (D-006) said the admin sees active vehicles "in real time". Add this to what we tell the client (OI-08).
  - No SignalR or polling in the API.

## D-036 — Self-signed certificate, app accepts any certificate

- **Date:** 2026-11-26
- **Category:** Security
- **Decision:** The API keeps its self-signed HTTPS certificate. The Flutter app accepts it through an `HttpOverrides` whose `badCertificateCallback` returns `true` for every certificate (owner's code).
- **Reasoning:** Owner's choice; no domain or public certificate needed. HTTPS stays, so `Secure` cookies work.
- **Alternatives considered:**
  - Certificate pinning: accept only our certificate's SHA-256 fingerprint (recommended; same effort; declined).
  - A domain name with a public certificate.
- **Final selection:** Accept all certificates.
- **Status:** Approved. **Security risk accepted by the owner.** Closes OI-14.
- **Impact:**
  - **Security risk:** the app doesn't check who it is talking to. On a shared or hostile network (public Wi-Fi, a compromised router), an attacker can pose as the server and read Staff/Admin IDs, passwords, session cookies and data, or change responses.
  - Pinning can be added later in the app alone, without any server change.
  - App store reviews may question disabled certificate checks (OI-05).


## D-037 — Admin ID format

- **Date:** 2026-11-26
- **Category:** Business logic
- **Decision:** Admin IDs are `ADM` + 2-digit year the account was created + 5-digit sequence, for example `ADM2600001`.
  - Generated by the API. Sequence numbers are never reused, including after deactivation.
  - Separate sequence from staff IDs. Login ignores letter case.
  - The staff ID prefix setting (D-011) can't be set to `ADM`, so the two can never collide.
- **Reasoning:** Owner's choice; mirrors the staff pattern (D-011).
- **Alternatives considered:** `ADM` + 3 digits.
- **Final selection:** `ADM` + YY + 5 digits.
- **Status:** Approved
- **Impact:** AUTH-03, admin management, first-admin seed.

## D-038 — Dev and test databases on the server

- **Date:** 2026-11-26
- **Category:** Process
- **Decision:** During development the API uses a separate `VehicleManagementDM_Dev` database on the server's SQL Server. Integration tests create a throwaway `VehicleManagementDM_Test_<id>` database there for each run and drop it at the end.
- **Reasoning:** Owner's choice.
- **Alternatives considered:** The SQL Server Express installed on the dev PC.
- **Final selection:** Server.
- **Status:** Approved
- **Impact:**
  - Connection strings live only in `dotnet user-secrets` (dev PC) or server environment variables, never in git.
  - Creating and dropping test databases needs `dbcreator` rights, so the dev/test login is separate from the app's least-privilege production login (OI-12).
  - Test runs touch the same SQL Server as production data: tests only ever drop databases whose names start with `VehicleManagementDM_Test_`.

## D-039 — Notifications: in-app list now, push later

- **Date:** 2026-11-26
- **Category:** Architecture
- **Decision:** Reminders and transfer alerts are stored as in-app notifications (NTF-03) and in the reminder log. Sending to phones (FCM/APNs) sits behind an `IPushSender` interface with a no-op implementation until a Firebase project exists.
- **Reasoning:** Owner's choice. Push needs a Firebase project and Apple keys that don't exist yet.
- **Alternatives considered:** Building FCM sending now.
- **Final selection:** In-app list now.
- **Status:** Approved
- **Impact:** NTF-01/NTF-02 (push) are delivered when push is plugged in. Until then staff only see reminders inside the app.

## D-040 — QR labels built in the app

- **Date:** 2026-11-26
- **Category:** Architecture
- **Decision:** The Flutter app draws QR codes and builds the PDF labels (single and bulk, QR-02/QR-03). The API returns label data: QR token, registration number, category and type.
- **Reasoning:** Owner's choice. No PDF or QR library on the server; nothing new to install or license.
- **Alternatives considered:** Server-side generation with NuGet QR + PDF libraries.
- **Final selection:** In the app.
- **Status:** Approved. Settles the "PDF generation" row of `REQUIREMENTS.md` §10.
- **Impact:** Phase 8 needs a Flutter QR and PDF package.

## D-041 — Hand-over needs nothing from the receiver

- **Date:** 2026-11-26
- **Category:** Business logic
- **Decision:** Staff A picks staff B and a reason; the trip moves to B immediately. B doesn't sign or accept.
- **Reasoning:** Owner's choice; matches D-020 and TRP-07 as written.
- **Alternatives considered:** B signs on A's phone; B accepts on their own phone (adds a pending state).
- **Final selection:** No action by B.
- **Status:** Approved
- **Impact:** No signature on hand-over segments. B must be an active staff member with no active trip or transfer (D-026).

## D-042 — API implementation plan

- **Date:** 2026-11-26
- **Category:** Architecture
- **Decision:** Build the API in milestones, with an owner review after each:
  - M0: docs (`DATABASE_DESIGN.md`, `API_DOCUMENTATION.md`).
  - M1: skeleton and cross-cutting (errors, logging, migrations, cookie auth, file store).
  - M2: master data.
  - M3: trips.
  - M4: transfers.
  - M5: notifications, reminders, dashboard, reports.
  - M6: release readiness.
  Photo uploads (vehicles, staff) are allowed up to **20 MB**; signatures up to 512 KB.
- **Reasoning:** Owner approved the plan on 2026-11-26 and set the 20 MB photo limit.
- **Alternatives considered:** —
- **Final selection:** As stated.
- **Status:** Approved
- **Impact:** Data model in `DATABASE_DESIGN.md`; endpoints in `API_DOCUMENTATION.md`; `ARCHITECTURE.md` §11 updated to 20 MB.

## D-043 — Data rules and database design approved

- **Date:** 2026-11-26
- **Category:** Database, Business logic
- **Decision:** Approve `DATABASE_DESIGN.md` (v0.2) with the answers to its questions:
  - **Q1 Vehicle after cancel / swap-out:** becomes `Available` at the lot the staff member picked, or "Not at a lot". The admin is notified and can then set Inactive or Under maintenance. Clarifies TRP-08 / TRP-07 ("status doesn't change; the admin decides").
  - **Q2 First reminder delay:** default 120 minutes; admins change it in Settings.
  - **Q3 Capacities:** per-type capacities don't have to add up to the total capacity.
  - **Q4 Status changes:** admins can set Inactive / Under maintenance only when the vehicle isn't on a trip or transfer (`STATUS_CHANGE_NOT_ALLOWED`).
  - The design's other proposals stand: enums stored as strings; lot types without a capacity row show occupancy with no per-type limit.
- **Reasoning:** Owner: "Accept all".
- **Alternatives considered:** Listed in `DATABASE_DESIGN.md` §7.
- **Final selection:** As proposed.
- **Status:** Approved. Milestone M0 is closed; M1 (skeleton) is next.
- **Impact:** `DATABASE_DESIGN.md` v0.2, `API_DOCUMENTATION.md`, `REQUIREMENTS.md` TRP-07/TRP-08.

## D-044 — Password length and unknown routes

- **Date:** 2026-11-27
- **Category:** Security
- **Decision:** Two defaults set while building M1, needing the owner's confirmation:
  - **Passwords:** 8 to 128 characters, no other complexity rules. Applies to change-password now and to admin-set passwords (staff, admins, first-admin seed) from M2.
  - **Unknown routes:** every controller endpoint still requires sign-in by default (`401 UNAUTHENTICATED` when signed out). A URL that matches no endpoint answers `404 ROUTE_NOT_FOUND` to everyone, signed in or not; a wrong HTTP method answers `405 METHOD_NOT_ALLOWED` to everyone.
  - *Proposed first (2026-11-27): signed-out callers get 401 even for unknown routes. Changed by the owner the same day.*
- **Reasoning:** No password rule was set in the requirements. 8 characters is the NIST SP 800-63B minimum, and length beats composition rules. Secure-by-default authorization is standard practice.
- **Alternatives considered:** Longer minimums or complexity rules; answering 401 to signed-out callers for unknown routes (hides which routes exist; declined by the owner).
- **Final selection:** Passwords 8–128 characters; unknown routes 404 for everyone.
- **Status:** Approved 2026-11-27. The owner reviewed D-044 and changed only the unknown-route part.
- **Impact:** `PasswordRules` in the API; authorization is applied with `MapControllers().RequireAuthorization()` instead of a fallback policy (a fallback policy also covers URLs that match no endpoint); `API_DOCUMENTATION.md` §2.

## D-045 — Settings limits and M2 scope notes

- **Date:** 2026-11-27
- **Category:** Business logic
- **Decision:** Defaults set while building M2, needing the owner's confirmation:
  - **Staff ID prefix:** 1 to 5 letters, stored upper-case; `ADM` is refused (D-037).
  - **First-reminder delay:** 1 to 1,440 minutes (24 hours). The 30-minute repeat stays fixed (D-009).
  - **Vehicle history** (`GET vehicles/{id}/history`) moves from M2 to M4, when trips and transfers exist; status and location changes are already recorded in the audit log from M2.
  - **Staff trips** (`GET staff/{id}/trips`) and the **hand-over picker** (`GET staff/handover-candidates`) move from M2 to M3, with trips.
  - **Newly assigned lots** for staff must be active; a lot that became inactive after it was assigned may stay on the staff member. *(Superseded by D-047: no assigned lots.)*
  - **Admin password reset** also unlocks the account (clears the lockout).
- **Reasoning:** The requirements give no limits for these settings; the scope moves avoid shipping endpoints that would return incomplete data.
- **Alternatives considered:** No upper limit on the delay; building history in M2 with status changes only.
- **Final selection:** As proposed.
- **Status:** Approved 2026-11-27 (owner: "Approve both", together with milestone M2).
- **Impact:** `API_DOCUMENTATION.md` §4.3, §4.7, §4.13; `ARCHITECTURE.md` §13.

## D-046 — Trip details settled in M3

- **Date:** 2026-11-27
- **Category:** Business logic
- **Decision:** Points the requirements leave open, as built in M3, needing the owner's confirmation:
  - **Origin lot:** always the lot the vehicle is parked at, which must be one of the staff member's lots. `originLotId` is optional; when sent, it must match. *(Superseded by D-047: any active lot; `originLotId` removed.)*
  - **Swap:** the new vehicle may come from any active lot (not only the staff member's lots), because the staff member is wherever the problem happened.
  - **Hand-over target:** any active staff member with no active trip; they don't need to be assigned to the origin lot.
  - **Cancel:** the note is required (TRP-08 says "reason + note").
  - **Places to leave a vehicle** (return lot, cancel lot, swap-out lot) must be active lots; "Not at a lot" is always allowed for cancel and swap.
  - **Overdue:** a trip is overdue once its first reminder time (start + reminder delay) has passed.
  - **Visibility:** staff see every trip they took part in, including ones they handed over (read-only); other people's trips answer 404. Admins see all.
  - **Monthly stats** (`auth/me`): trips the staff member took part in during the GST calendar month, and their own driving time in it.
  - **Not yet:** the admin notification on cancel and the reminder log come with notifications (M5); transfer checks (D-026, `VEHICLE_IN_TRANSFER`) come with transfers (M4).
- **Reasoning:** Needed to build the endpoints; each follows the closest requirement or decision.
- **Alternatives considered:** Swap only from the staff member's lots; hand-over only to staff of the same lot; optional cancel note.
- **Final selection:** As proposed.
- **Status:** Approved 2026-11-27 (owner: "Approve both", together with milestone M3).
- **Impact:** `API_DOCUMENTATION.md` §4.2, §4.9.

## D-047 — Staff have no assigned lots

- **Date:** 2026-11-27
- **Category:** Business logic
- **Decision:** Staff members (who are the drivers, D-020) are not tied to any parking lot. Lot assignment is removed completely:
  - **Start:** any active staff member can start a trip with any Available vehicle parked at any **active** lot. The origin lot is always the lot the vehicle is parked at; the "pick origin lot" step (S-02) is removed.
  - **Transfers:** any active staff member can confirm a transfer's departure (and so drive it) or its arrival. "Transfer waiting" notifications go to all active staff (M4/M5).
  - **Accounts:** no lots on staff create/edit, staff list, staff detail or `auth/me`; the `StaffLots` table is dropped (migration `RemoveStaffLots`).
  - Error code `LOT_NOT_ASSIGNED` is removed; `NOT_ORIGIN_LOT_STAFF` is dropped from the transfer contract.
- **Reasoning:** Owner instruction: "staff member doesn't have lot assigned, staff is driver", then "remove completely".
- **Alternatives considered:** Keeping lots on the staff profile as information only, without restricting anything.
- **Final selection:** Remove completely.
- **Status:** Approved
- **Supersedes:** the "lots for staff" part of D-009; BR-03 (start part); STF-01 and P-01 (assigned lots); P-04 ("staff of the origin lot"); TRP-01 step 1; the "staff of the origin lot" wording in D-020 and TRF-02; the lot points in D-045 and D-046; UX screen S-02.
- **Impact:**
  - API: staff endpoints and `auth/me` lose their lot fields; start no longer accepts `originLotId` (API_DOCUMENTATION.md v0.6).
  - UX/mock-ups: Profile (S-24), staff form/detail (A-16) and the start flow lose assigned lots and the lot step. The rendered PDF still shows them until Phase 4 re-renders.

## D-048 — Details settled in M4–M6

- **Date:** 2026-11-27
- **Category:** Business logic, Deployment
- **Decision:** Points the requirements leave open, as built after the owner asked to "complete all remaining work":
  - **Transfers:**
    - Inactive or under-maintenance vehicles can't be transferred (VEH-03).
    - Departure re-checks that the vehicle is still Available and at the origin lot.
    - Arrival is allowed even if the destination lot was deactivated after creation (the vehicle is physically there).
    - Admins can't move a vehicle by hand while it has an open transfer.
    - Staff can view any transfer.
  - **Notification audiences (no lot link, D-047):**
    - "Transfer waiting": all active staff.
    - "Transfer on the way": all active staff except the driver.
    - Trip cancelled and vehicle swapped out: all active admins (D-043).
    - Reminders: the driver plus all active admins, every 30 minutes after the first. Slots missed while the server was down are skipped, not sent in a burst.
  - **Hours:** transfer driving time counts in a staff member's hours (TRF-05), including `auth/me` monthly stats and the staff hours report.
  - **Dashboard ("today" = current GST day):**
    - Trips today: trips started today.
    - Hours out today: trip and transfer time within today.
    - Free spaces: the sum of positive availability over active lots.
    - "Needs attention" lists: at most 20 items each, with full counts.
  - **Reports:**
    - R-1 and R-2 need a date range of at most 366 days.
    - R-1's lot filter means trips started from, and transfers leaving, that lot.
    - R-3 shows current occupancy plus movements in the period (default today).
    - R-4 and R-5 are paged lists.
  - **Deployment** (DEPLOYMENT.md):
    - A separate IIS site and app pool on the existing server, set to AlwaysRunning so reminders keep running.
    - Database `VehicleManagementDM` with its own login `vms_app` (db_owner of that database only).
    - Data folders outside the site folder.
    - Secrets in server environment variables.
- **Reasoning:** Needed to finish M4–M6; each follows the closest requirement or decision.
- **Alternatives considered:** Notifying only staff "near" a lot (impossible without lot links); a fixed reminder burst after downtime; optional report ranges.
- **Final selection:** As listed; approved by the owner 2026-11-27.
- **Status:** Approved
- **Impact:** `API_DOCUMENTATION.md` v1.0 §4.10–§4.13; `DEPLOYMENT.md`.

## D-049 — Controllers in one `Controllers/` folder

- **Date:** 2026-11-27
- **Category:** Architecture
- **Decision:** All API controllers move to `backend/src/VehicleManagementDM.Api/Controllers/`, one controller per file (`AdminsController` split out of `StaffController.cs`), namespace `VehicleManagementDM.Api.Controllers`. Services, rules and DTOs stay in `Features/<Area>/`. Request/response classes that were declared inside the Destinations, Settings and VehicleCategories controller files moved to `<Area>Dtos.cs` in their feature folder.
- **Reasoning:** Owner asked for it: one folder listing every endpoint is easier to read.
- **Alternatives considered:** Controllers inside feature folders (the previous layout); a full `Controllers/` + `Models/` split.
- **Final selection:** Controllers only; models/DTOs unchanged.
- **Status:** Approved (owner request)
- **Impact:** Code layout only; no route, behaviour or contract change. `ARCHITECTURE.md` §1 updated. 105 tests pass.

## D-050 — Hosting on the app server

- **Date:** 2026-11-27
- **Category:** Deployment
- **Decision:**
  - The API runs in **IIS** on the same app server as the Sufizz Bikes backend, in its own site and app pool, on **port 8445** (HTTPS).
  - Server runtime: ASP.NET Core **8.0.18** (`dotnet --list-runtimes`). The framework-dependent build runs on it; nothing to install.
  - The owner creates the first admin account (via the API seed or SQL) at deploy time.
  - Server setup (DEPLOYMENT.md §3) is done by the owner at deploy time.
- **Reasoning:** Owner's answers; matches D-028 (nothing new installed on the server).
- **Status:** Approved (owner answers)
- **Impact:** Resolves OI-13. `DEPLOYMENT.md` §1, §3.3, §7. The Flutter app's base URL is the server address on port 8445 (kept out of the public repo).

## D-051 — Passwords stored as plain text

- **Date:** 2026-11-27
- **Category:** Security
- **Decision:**
  - `Users.PasswordHash` (PBKDF2 via `PasswordHasher<T>`) is replaced by `Users.Password`, nvarchar(128), plain text.
  - Sign-in and password change compare in code (`Passwords.Matches`: constant-time, case-sensitive), never in SQL, because the database collation is case-insensitive.
  - Passwords are still never returned by the API, logged or written to the audit log. Lockout, rate limiting and session rules (D-031) are unchanged.
  - Migration `PlainTextPasswords` drops the old hashes: existing accounts get an empty password that never matches and must have their password set again (SQL or reset-password).
  - The first admin can be created with a SQL script (DEPLOYMENT.md §3.4).
- **Reasoning:** Owner's choice, so admins can be created and passwords set directly with SQL.
- **Risk (explained to and accepted by the owner):** anyone who can read the database, a backup or an exported copy sees every staff and admin password, and people often reuse passwords for other accounts. Mitigations: SQL access only for trusted people, the app's own SQL login (OI-12), protected backup files.
- **Alternatives considered:** keep hashing and provide a SQL script with a pre-computed hash; first admin via the seed variables (still available).
- **Status:** Approved (owner, after the risk was explained)
- **Impact:** Replaces "Passwords stored with a modern adaptive hash" (REQUIREMENTS.md §8) and ARCHITECTURE.md §4 "Passwords". DATABASE_DESIGN.md Users table; DEPLOYMENT.md §3.4.

## D-052 — Production database login and settings file

- **Date:** 2026-11-27
- **Category:** Deployment, Security
- **Decision:**
  - The API connects to `VehicleManagementDM` on the app server's SQL Server with the **`sa`** login (`Server=localhost,1433`). The API creates the database and tables on first start (D-030).
  - Server settings (connection string, data folders) live in `appsettings.Production.json` in the site folder. It is git-ignored and never published (`CopyToPublishDirectory="Never"`), so deploys don't overwrite it. The owner fills in the `sa` password before deploy.
- **Reasoning:** Owner's choice.
- **Risk (explained to and accepted by the owner):** the `sa` password sits in plain text in the settings file; anyone who reads it controls every database on that SQL Server (including the Sufizz Bikes one) and can run commands on the server. Port 1433 is reachable from the internet. With plain-text passwords (D-051), `sa` also exposes every user's password. Mitigations: long random `sa` password, port 1433 limited to known IPs, read access to the site folder limited.
- **Alternatives considered:** a dedicated `vms_app` login with rights on this database only (DEPLOYMENT.md §3.2), still possible later by changing the connection string.
- **Status:** Approved (owner, after the risk was explained). Resolves OI-12.
- **Impact:** DEPLOYMENT.md §3.2, §3.3, §7; `.gitignore`; API project file.

## D-053 — Two mobile apps sharing one core

- **Date:** 2026-11-27
- **Category:** Architecture
- **Decision:**
  - Two Flutter apps: **Staff** ("DM Staff", `com.dm.vms.staff`) and **Admin** ("DM Admin", `com.dm.vms.admin`). Names and IDs can change before a store release.
  - One folder `app/` (a pub workspace): `packages/dm_core` holds what both share (API client, session, models, theme, shared widgets); `apps/staff` and `apps/admin` hold the screens.
  - Staff app first; the admin app follows.
  - The API is unchanged: it already checks every permission by role. The staff app refuses admin accounts and the admin app refuses staff accounts.
- **Reasoning:** Owner's choice. Each app carries only its own screens; the shared package avoids writing the API client, sign-in and theme twice.
- **Alternatives considered:** one app with roles (D-007); two independent projects; one project with two build flavors.
- **Status:** Approved. Supersedes D-007.
- **Impact:** UX_ARCHITECTURE.md stays valid per role (staff tabs, admin tabs); "role-based navigation" now means one app per role.

## D-054 — Start the Flutter phase now

- **Date:** 2026-11-27
- **Category:** Process
- **Decision:** Start Phase 8 (Flutter) with the staff app as a first cut, although Phase 4 (remaining high-fidelity screens) is not complete. The owner approved the UX structure (D-014) and the Emerald Ops design (D-017). Screens without a mock-up follow the design system and UX_ARCHITECTURE.md.
- **Reasoning:** Owner's request ("start building app now … first cut rough will be okay"). Same kind of deviation from D-005 as D-027 was for the backend.
- **Status:** Approved

## D-055 — Mobile stack and toolchain

- **Date:** 2026-11-27
- **Category:** Technology
- **Decision:**
  - **Toolchain (owner's working setup, pinned):** Flutter 3.35.3 stable / Dart 3.9.2 at `E:\downloads\flutter_windows_3.35.3-stable\flutter` (no `flutter upgrade`); JDK = Android Studio JBR 21; compileSdk/targetSdk 36; NDK 28.2.13676358; Android Gradle Plugin 8.7.2; Gradle 8.10.2; Kotlin 2.1.20; Java 17; `PUB_CACHE=E:\flutter_pub_cache`. Android only for now; iOS later on a Mac.
  - **Packages:** flutter_riverpod (state), go_router (navigation), dio + cookie_jar + dio_cookie_manager + path_provider (API and session cookie), mobile_scanner (QR), signature (signature pad), flutter_svg, intl, url_launcher. Models written by hand (no code generation).
  - **Icons:** Flutter's built-in Material Icons, rounded style (outlined variants for inactive tabs). *Changed 2026-11-27 after the phone test: the `material_symbols_icons` package's fonts lose most outline icons when a release build shrinks icon fonts; keeping them would need `--no-tree-shake-icons` (+15 MB APK). The built-in rounded set looks almost the same and shrinks correctly. This adjusts the design's "Material Symbols Rounded" (D-017) for the apps only.*
  - **Configuration:** the API base URL comes from `--dart-define-from-file`; the real file is git-ignored (public repo). The self-signed certificate (D-036) is accepted only for the configured API host.
  - **Release signing:** debug keystore for sideloading; a real keystore is needed before Play Store.
- **Reasoning:** Matches the owner's other Flutter project that builds on this PC; well-known, maintained packages; no code generation keeps the build simple.
- **Status:** Approved (toolchain from the owner; packages as planned and approved 2026-11-27)
