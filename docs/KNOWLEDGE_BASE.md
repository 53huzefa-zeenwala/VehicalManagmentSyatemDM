# Project Knowledge Base

**Current state as of 2026-11-26.** This file holds everything known about the project in one place, reflecting every change up to decision D-023. Older documents may still contain superseded lines; when they disagree, this file and `PROJECT_DECISIONS.md` win.

**Reading order for a new session:**
1. This file
2. `PROJECT_DECISIONS.md` (source of truth)
3. `REQUIREMENTS.md` v1.1 (§0 lists the changes)
4. `docs/ux/UX_ARCHITECTURE.md`
5. `docs/design/DESIGN_SYSTEM.md`
6. `docs/design/mockups/UI_Mockups_v1.pdf`

---

## 1. The product in one paragraph

One mobile app (Flutter, Android and iOS) with two roles, Staff and Admin, on one ASP.NET Core Web API (.NET 8) and one Microsoft SQL Server database. There is no web portal.

- **Staff** take municipal fleet vehicles (cars, garbage trucks, ambulances…) out of parking lots and bring them back. **The staff member is the driver.** The app records who took which vehicle, from which lot, to which destination and for how long. A live timer runs until the vehicle is back.
- **Admins** set up the master data (vehicles, staff, lots, destinations), create transfers between lots, and **monitor** activity live and in reports. They never act on trips themselves.

Client context (inferred): a Dubai municipal fleet. Evidence: "DM" branding and Staff IDs, Dubai locations, +971 numbers. Single organisation, timezone GST (UTC+4), English in v1, with layouts ready for right-to-left (Arabic later).

## 2. Roles

| | Staff (driver) | Admin |
|---|---|---|
| Who | Field staff on their **personal phones**. Each staff member drives the vehicles they take out | Supervisors; more admin accounts can be added. Our team creates the first one |
| Login | Staff ID (`DM` + YY + 5 digits, e.g. `DM2600042`, generated) + password issued by the admin | Admin ID + password |
| Can | Scan a vehicle; start, swap, cancel and end **their own** trips; confirm transfers; see lots and space; see their own trips and profile | Manage vehicles, categories and types, QR labels, staff, admins, lots, destinations and settings; create transfers; see live trips, the dashboard, reports and the audit log; resolve "Not at a lot" vehicles and set vehicle status |
| Cannot | Manage master data; see other people's trips | **End, swap or cancel trips; scan QR codes** (monitor only, D-021) |

## 3. Scope (v1)

**In scope**
- Authentication
- Staff management (with driving licence), admin management
- Vehicle catalogue (Category → Type), QR codes (generate, image or PDF label, bulk print)
- Parking lots (capacity per type, baseline count, map), destinations (Area → Location)
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

**Start** (staff, from an assigned lot only):
1. Origin lot (skipped when the staff member has only one).
2. Vehicle: scan the QR code, or type the registration number.
3. Destination: area → location.
4. **Staff signature**: the staff member signs to confirm taking the vehicle.
5. Review → Start. The server records the start time.

**Checks at start:**
- The vehicle is Available and located at the origin lot.
- The staff member has no other active trip.
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

**Open risk OI-10:** nobody except the staff member can close a trip.

### 4.2 Hours

- Hours are counted by segment, for each **staff member** and each **vehicle**. "Driver hours" are the same as staff hours.
- Cancelled trips count.
- Hours that cross midnight are split by calendar day in reports.
- All times come from the server clock, are stored in UTC and shown in GST.

### 4.3 Lots and capacity

- **Fields:** name, unique code, address, Google Maps pin, contact person and number, opening and closing time (information only), type (Open, Underground or Other), total capacity, capacity per vehicle type, status (Active/Inactive), baseline occupied count.
- **Occupancy:** occupied = baseline + vehicles located at the lot and not on a trip + vehicles in a transfer that hasn't arrived yet. Available = capacity − occupied, per type and in total.
- **Full lot:** a warning, never a block.
- **Inactive lots:** can't be used for start, return or transfer.
- **Map view:** for staff and admins.

### 4.4 Transfers

- The admin creates a transfer: vehicle + destination lot, with an optional note. **No driver is chosen.**
- Any staff member of the origin lot confirms departure, and **whoever confirms it drives**. Hours start then.
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

- Staff profile fields: full name, photo, mobile, optional email, **driving licence number**, assigned lots (one or more), Staff ID.
- The admin resets staff passwords; our team resets the first admin's. There's no forced password change at first login. Users can change their own password.
- Lockout after 5 failed attempts, for 15 minutes.
- Deactivated accounts end their sessions immediately.
- Records are **deactivated, never deleted**.

### 4.7 Reports and dashboard (admin)

- **Dashboard:** live count of vehicles on trip, a trips-per-hour chart, trips today, hours out, free spaces, then "Needs attention" (overdue trips, vehicles not at a lot, lots over capacity, transfers waiting).
- **Live tab:** trips (overdue first) and transfers.
- **Trip detail:** read-only, with staff, vehicle, route, segments and the reminder log.
- **Reports** (on screen with filters, no export): vehicle running hours, staff activity/hours, lot utilization, trip log (including cancelled and handed-over trips), transfers.
- **Audit log:** every admin create, edit, deactivate, status change and transfer.

## 5. Navigation and screens

- **Staff tab bar:** Home · Trips · **Scan** (raised centre button) · Lots · **Profile**.
- **Admin tab bar:** Dashboard · Live · Manage · Reports. No scan button.

**Designed screens** (in the PDF, light and dark):
- **Staff:** Home (ready), Scan, Start: Vehicle (1/4), Destination (2/4), Signature (3/4), Review (4/4), Home (trip running), Active trip (overdue, hand-over in the timeline), Swap, End: return lot, Trip summary, Cancel, My trips, Lots (map + list), Profile, Sign in.
- **Admin:** Dashboard, Live, Trip detail (read-only), New transfer, Manage hub, Vehicle detail (resolve "Not at a lot", QR), Lot detail, Report.

**Still to design** (Phase 4): the remaining list and form screens (vehicle, staff, lot, destination and category forms; staff detail; admin management; settings; audit log; notifications; transfer confirm for staff; QR bulk print), plus empty, error, offline and permission states.

## 6. Design system: "Emerald Ops" (D-017, proposed)

- **Colours:**
  - Emerald primary `#0B7A62` (dark mode: mint `#34D3A6`)
  - Hero gradient `#06503F → #0B7A62 → #0C7F64`
  - **Amber `#FFB020` = live only** (timer bar, on trip)
  - Danger `#D92D20`, warning `#B54708`, info `#175CD3`
  - Light background `#F3F6F5`; night dark background `#0A1214`
  - All text pairings pass WCAG AA (computed).
- **Type:** Plus Jakarta Sans (with tabular figures for timers). Arabic later: pair with IBM Plex Sans Arabic or Noto Kufi Arabic.
- **Icons:** Material Symbols Rounded.
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
| D-007 | One app, role from the account | Approved |
| D-008 | .NET 8 (end-of-life risk accepted) | Approved |
| D-009 | Business rules batch 2 | Approved; driver and admin-end parts superseded |
| D-010 | Business rules batch 3 | Approved; driver and attendance parts superseded |
| D-011 | Staff ID format `DM`+YY+5 digits | Approved |
| D-012 | Business rules batch 4 (segments, transfer hours, RTL-ready, on-screen reports) | Approved; Driver ID and attendance parts superseded |
| D-013 | `REQUIREMENTS.md` v1.0 + P-01…P-22 | Approved; P-02 and P-11 superseded |
| D-014 | UX structure UX-01…UX-12 | Proposed |
| D-015 | "Fleet Signage" design | Rejected |
| D-016 | Review UX and design together on themed screens | Approved |
| D-017 | "Emerald Ops" design | Proposed |
| D-018 | Deliverables = dummy app → PNG → PDF; knowledge base | Approved |
| D-019 | Scan button in both tab bars | Superseded by D-022 |
| D-020 | **The staff member is the driver**, staff signature kept, swap = vehicle or hand-over, transfer driver = whoever confirms departure | Approved |
| D-021 | **Admins monitor only**, no admin scanner | Approved |
| D-022 | Staff tabs Home·Trips·Scan·Lots·Profile; admin 4 tabs | Approved |
| D-023 | **Attendance removed** | Approved |

## 8. Phase status

| Phase | Status |
|---|---|
| 0 Requirement clarification | ✅ Done |
| 1 Business analysis | ✅ Done (`REQUIREMENTS.md` v1.1) |
| 2 UX / IA + 3 Design system | 🟡 In review together (D-016). Waiting on D-014 and D-017 |
| 4 High-fidelity UI (remaining screens and states) | Next, after approval |
| 5–10 Architecture, database, backend, Flutter, QA, deployment | Not started |

## 9. Open items

| ID | Item | Needed by |
|---|---|---|
| OI-01 | Scale: vehicles, lots, staff, trips per day | Phase 5 |
| OI-02 | Privacy policy text and retention. Smaller now: no attendance photos or GPS; signatures and phone numbers remain | Phase 7 |
| OI-03 | Owner of the Google Maps API key and billing | Phase 7 |
| OI-04 | App name, icon, logo (placeholder "Fleet & Parking") | Phase 4 |
| OI-05 | App store accounts and distribution | Phase 10 |
| OI-06 | Hosting and SQL Server edition | Phase 5 |
| OI-07 | Development machine (deferred by the owner) | Phase 7 |
| OI-08 | Tell the client: one app with roles, not two apps | Before Phase 4 sign-off |
| OI-09 | .NET 8 is past end of support (10 Nov 2026); revisit | Before Phase 7 |
| OI-10 | **Who closes a trip if its staff member can't** (admins are monitor-only) | Before Phase 5 |

## 10. Environment and tooling

- **Machine:** Apple Silicon Mac, macOS 15.5.
- **Not installed:** .NET SDK, Flutter, Xcode (Command Line Tools only), Android SDK, JDK, Docker, GitHub CLI (`gh`), Homebrew.
- **Available:** git 2.39, Google Chrome 157 (headless), Python 3.14 + Pillow 12.2, macOS PDFKit via JXA (used to read and preview PDFs).
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
| `2026-11-26-…vehicleandpar.txt` (repo root) | The owner's export of the full working session (raw history) |

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
| 2026-11-26 | Knowledge base rewritten to the current state; project put under git and pushed to GitHub |

## 13. Owner preferences

- Reports must be **extremely concise**.
- Ask **one** clarifying question when a request is ambiguous.
- Production quality.
- Prefers **forms with selectable options** for questions, in short rounds.
- Wants **realistic, good-looking UI shown as a PDF of rendered screens**, not greyscale wireframes or HTML preview pages.
- Takes part in every important decision; every decision is logged.
- Makes product changes directly ("driver is staff", "remove attendance"). Record each one as a decision and propagate it to every doc and screen.
