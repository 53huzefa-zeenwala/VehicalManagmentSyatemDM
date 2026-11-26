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
| D-007 | 2026-11-18 | Architecture | One mobile app; the role (Admin or Staff) comes from the account | Approved |
| D-008 | 2026-11-18 | Technology | Keep .NET 8 for the API (end-of-life risk accepted) | Approved |
| D-009 | 2026-11-18 | Business logic | Business rules batch 2 (owner answers, form 3) | Approved; driver and admin-end parts superseded by D-020, D-021 |
| D-010 | 2026-11-22 | Business logic | Business rules batch 3 (owner answers, form 4) | Approved; driver parts superseded by D-020 |
| D-011 | 2026-11-22 | Business logic | Staff ID format `DM` + YY + 5-digit sequence | Approved 2026-11-22 |
| D-012 | 2026-11-22 | Business logic | Business rules batch 4 (form 5 + final questions) | Approved; Driver ID part superseded by D-020 |
| D-013 | 2026-11-22 | Process | `REQUIREMENTS.md` v1.0 approved, including P-01 to P-22; Phase 1 closed | Approved; P-02, P-11 superseded (D-020, D-021) → v1.1 |
| D-014 | 2026-11-22 | UX/UI | UX structure: role-based navigation, active-trip bar, flows (UX-01 to UX-11) | Proposed |
| D-015 | 2026-11-22 | UX/UI | Design system v0.1 "Fleet Signage": tokens, fonts, icons, components | Rejected |
| D-016 | 2026-11-22 | Process | Review the UX structure and design system together, on themed screens | Approved |
| D-017 | 2026-11-22 | UX/UI | New visual direction "Emerald Ops" (colours chosen by Claude, as the owner delegated) | Proposed |
| D-018 | 2026-11-22 | Process | Design deliverables = dummy app → screenshots → PDF; project knowledge base | Approved |
| D-019 | 2026-11-22 | UX/UI | Centre Scan button in both tab bars (UX-12) | Superseded by D-022 |
| D-020 | 2026-11-23 | Business logic | The staff member is the driver; no separate driver records | Approved |
| D-021 | 2026-11-23 | Business logic | Admins monitor only: no ending, swapping, cancelling or scanning | Approved |
| D-022 | 2026-11-23 | UX/UI | Tab bars: Staff = Home · Trips · Scan · Lots · Profile; Admin = 4 tabs, no Scan | Approved |
| D-023 | 2026-11-23 | Business logic | Attendance removed from the app (v1 scope) | Approved |

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
- **Status:** Approved. Supersedes the "two dedicated apps" part of D-001.
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
  - **Lots for staff:** staff can start vehicles only from their assigned lots. At End, staff choose the lot the vehicle returned to, which can be any lot.
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

## D-014 — UX structure (proposed)

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
- **Status:** Proposed
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

## D-017 — Visual direction "Emerald Ops" (proposed)

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
- **Status:** Proposed
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
  - **Transfers:** no driver is chosen upfront. Any staff member of the origin lot can confirm departure, and whoever confirms it drives the vehicle. Arrival is confirmed by any staff member.
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
