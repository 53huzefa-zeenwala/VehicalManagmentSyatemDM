# Project Overview — Vehicle & Parking Management System

> **2026-11-22:** this is the early understanding, kept for history. The current spec is `REQUIREMENTS.md`, and the source of truth is `PROJECT_DECISIONS.md`.

Status: **Phase 0: requirement clarification.** Based on `docs/source/requirements-v1-revised.pdf` (3 pages, read in full) and the palette references in `docs/source/palette/`.
Legend: **[C]** confirmed by the document · **[A]** my assumption, not yet confirmed · **[?]** unknown; see `OPEN_QUESTIONS.md`.

## 1. What we are building

- [C] ~~Two mobile apps, Admin App and Staff App~~. Superseded by **D-007**: **one app** for Android and iOS, with features decided by the account's role (Admin or Staff).
- [C] Confirmed business rules: see D-006 (client) and D-009 (owner) in `PROJECT_DECISIONS.md`. Where they differ from sections 3–7 below, the decisions win.
- [C] One centralized backend, database and API shared by both apps, so admins and staff see the same data.
- [C] Mobile only. No web portal or website.

## 2. Problem it solves

- [C] Admins need one place to manage staff, vehicles, parking locations, vehicle QR codes, transfers between lots, lot utilization and vehicle operating hours.
- [C] Staff need a quick way to identify a vehicle (by scanning its QR code) and record when its operation starts and ends.
- [A] Today this is probably tracked manually (paper or spreadsheets), so hours, locations and lot occupancy are unreliable. The document does not say this.
- [?] What industry this is, whose vehicles they are, and what "operating" a vehicle means (A1).

## 3. Users

| User | App | What they do |
|---|---|---|
| Admin [C] | Admin App | Manages staff, vehicle catalogue, QR codes, lots and transfers; views dashboard and reports |
| Staff [C] | Staff App | Scans vehicle QR, views vehicle and parking info, starts and ends vehicle operation, views activity |
| Super Admin [?] | Admin App | Implied by "Admin account management" but never defined (B1) |
| Driver [mockup] | None (identified by scan) | Takes a vehicle out to a work location and signs at Start. Not in the requirements PDF (N2) |

## 4. Modules

**Admin App** [C]
1. Login & authentication, admin account management
2. Dashboard & reporting: totals, active and completed records, staff overview, lot utilization, occupied and available capacity, running-hours reports, staff activity reports
3. Staff management: add, view, edit, delete; login credentials and access
4. Vehicle category & type management, vehicle records
5. QR code management: one unique QR per vehicle, linked automatically, individual download, identification by scan
6. Parking lot management: multiple lots, details, capacity, occupancy, utilization
7. Vehicle transfer management: move between lots, update location, transfer history, current location
8. Vehicle time management: start and end times, automatic hours, usage history, time reports

**Staff App** [C]
1. Login, profile view, logout
2. QR scanner: scan, fetch vehicle info automatically
3. Vehicle details: info, category and type, identification details, current status
4. Parking details: assigned lot details, vehicle's current location
5. Vehicle start & end: automatic start and end time recording, automatic hours
6. Activity tracking: running duration, hours per staff member, activity records, all start and end transactions

## 5. How it works (major journeys)

1. **Setup (Admin)** [C]: create categories and types → add lots with capacity → add vehicles (each gets a QR automatically) → download and print QR → add staff with credentials. [?] Assign staff to lots (B2).
2. **Daily operation (Staff)** [C]: log in → scan vehicle QR → see vehicle, status and location → **Start** (time recorded) → … → scan again → **End** (time recorded, hours calculated).
3. **Transfer (Admin)** [C]: choose vehicle → choose destination lot → location updated, history recorded. [?] Whether staff confirm the physical move (D2).
4. **Monitoring (Admin)** [C]: dashboard for totals, occupancy and active vehicles; reports for running hours and staff activity.

## 6. What happens behind the scenes

- [C] The central API authenticates admins and staff separately and serves both apps from one database.
- [C] A scan resolves the QR code to a vehicle record.
- [C] Start creates an activity record. End closes it and calculates the duration automatically.
- [C] Each vehicle has one current lot. Transfers change it and add an entry to the transfer history.
- [C] Lot occupied and available counts are derived from vehicles and lot capacity.
- [A] Times come from the server clock, not the phone, to prevent tampering.
- [?] Whether a running vehicle still occupies its space (C1).
- [?] What happens to sessions that are never ended (C4).
- [?] Offline behaviour (G1).

## 7. Information to store (high level, not a database design)

- [C] Admin accounts; staff accounts (profile, credentials, access); vehicle categories and types; vehicles (identification, category and type, status, current lot, QR); parking lots (details, capacity); start and end activity records (vehicle, staff, times, duration); transfer history (vehicle, from, to, when, by whom).
- [A] Audit trail of admin changes (who changed what and when), recommended for production.
- [?] Staff-to-lot assignments (B2); extra Start/End data such as meter readings or photos (C5); vehicle documents (E1).

## 8. Integrations & external dependencies

- [C] Device camera (QR scanning); file save and share (QR download); Apple App Store and Google Play distribution (Android and iOS).
- [?] Email or SMS (password reset, OTP), push notifications (FCM/APNs), report export (PDF/Excel). None are mentioned in the document.

## 9. Environment check (2026-11-15, this Mac: Apple Silicon, macOS 15.5)

| Tool | Status |
|---|---|
| .NET SDK | **Not installed** (the user believed .NET 8 was available) |
| Flutter / Dart | **Not installed**. PATH points to `~/Flutter Projects/flutter/bin`, which does not exist |
| Xcode | **Not installed** (Command Line Tools only). Required for iOS builds |
| Android SDK / JDK | **Not installed**. PATH points to `~/Android/sdk`, which does not exist |
| Docker / SQL Server tools | **Not installed** |

Version facts:
- .NET 8 and .NET 9 reached end of support on 10 Nov 2026.
- .NET 10 is the current LTS release, supported until Nov 2028.
- The latest Flutter stable confirmed is 3.47 (Aug 2026), with 3.50 due in Nov 2026.
- SQL Server has no native macOS version. On Apple Silicon it runs in Docker (amd64 image under Rosetta), which is fine for development.

## 10. Palette analysis (historical: superseded by D-017 "Emerald Ops" on 2026-11-22)

Reference images: `palette-dark-reference.jpg` (image 1, dark UI) and `palette-light-reference.jpg` (image 2, light UI). The 60/30/10 split is stated in the images.

| Token | Value | Source |
|---|---|---|
| Canary Flash (accent, 10%) | `#FFE014`, RGB 255 224 20 | Stated in image |
| Ink Heather (main, 60%) | ≈ `#262A33` (surface), card ≈ `#20242D` | Sampled from image 1, estimate |
| Putty Grey (secondary, 30%) | ≈ `#C1BBAF` | Sampled from image 1, estimate |
| Light background | ≈ `#FBF8F1` (warm off-white) | Sampled from image 2 |
| Light text (ink) | ≈ `#21201E` | Sampled from image 2 |

Contrast checks (WCAG 2.x; text needs ≥ 4.5:1, large text and UI parts ≥ 3:1):

| Pair | Ratio | Verdict |
|---|---|---|
| Canary on light background | 1.24:1 | Fails. No Canary text, icons or thin strokes in light theme |
| Ink text on Canary | 12.3:1 | Passes. Canary buttons need dark labels |
| Canary on Ink Heather | 10.9:1 | Passes. Canary text and icons are fine in dark theme |
| Putty on Ink Heather | 7.5:1 | Passes. Putty works as text in dark theme |
| Putty on light background | 1.80:1 | Fails. Putty only for surfaces and borders in light theme |
| Ink on light background | 15.4:1 | Passes |

Observations:
- Image 2 labels a light-beige swatch as "Ink Heather". That contradicts image 1, so the light theme needs an interpretation (I2).
- Yellow conventionally means "warning". Status colours (available, occupied, error) need their own muted semantic set so they don't clash with the accent. To be handled in Phase 3.

## 11. Staff UI mockup (2026-11-18, reference only)

Source: `docs/source/staff-ui-mockup.pdf`, 31 rough screens in a dark theme (navy + yellow). The user does not want this UI copied; it shows how the workflow is meant to go.

- **Login:** User ID + password, "Remember me", and a Staff/Admin toggle (conflicts with two apps, N1).
- **Home ("Action"):** Start Vehicle · End Vehicle · Active Vehicle · Report · Attendance. Bottom navigation: Home · Profile · Settings.
- **Start:** scan the driver QR (or type the Driver ID) → scan the vehicle QR (or type the Vehicle ID) → driver signs on the phone → confirm the driver and vehicle cards → origin lot (pre-filled) + choose a destination location → Start now.
- **Active vehicles:** list → detail (start time, vehicle, driver) → Modify or Cancel.
- **Modify:** swap the vehicle, the driver, or both (by scan or ID) → "Start again".
- **Cancel:** scan the driver and vehicle → pick a reason (driver, vehicle, technical or other issue) + a written note.
- **End:** scan the driver and vehicle → confirm → summary (start, end, total time, driver, vehicle, zone, area) → "Vehicle received".
- **Driver profile:** photo, name, mobile, email, designation, employee code, licence number, address.
- **Vehicle details:** number, status, name, plate, start time, current location.
- **Attendance:** check in, check out, lunch and tea break, each with a photo, timestamp and automatic GPS. The schedule shows present, absent and late counts and a timeline of the day.
- **Context:** "DM" branding, a "Municipality Parking Lot", Dubai work locations and +971 numbers suggest a Dubai municipality fleet (A1, needs confirming).
