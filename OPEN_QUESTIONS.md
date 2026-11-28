# Open Questions, Gaps & Assumptions

**How to answer:** reply with the ID and your choice, e.g. `A1: a`, `C5: clock time only`, or `accept ★ for B, D, E`. ★ = my recommendation. Answers are recorded here and in `PROJECT_DECISIONS.md`.
**Priority:** Critical (blocks requirements or UX) · Important (needed before its phase) · Optional (preference).

---

## Owner changes — 2026-11-23

Recorded as D-020, D-021 and D-022. `REQUIREMENTS.md` is now v1.1 (§0).

| Topic | Answer |
|---|---|
| Driver | The staff member is the driver. No driver records or picker (D-020) |
| Signature | Kept. The staff member signs at start |
| Swap | Change the vehicle, or hand the trip over to another staff member |
| Transfer driver | Any staff of the origin lot; whoever confirms departure drives it |
| Admin role on trips | Monitor only: no end, swap, cancel or scan (D-021) |
| Menu | Staff: Attendance tab replaced by Profile. Admin: no Scan button (D-022) |

**New open item OI-10:** with admins on monitor only, who closes a trip if its staff member can't?

**Later the same day:** attendance was removed from the app entirely (D-023).

---

## Owner answers — 2026-11-22 (form 5 + final questions)

Recorded as D-011 (approved) and D-012. **Requirement clarification is closed.** Anything still open is tracked in `REQUIREMENTS.md` §11.

| ID | Answer |
|---|---|
| P1 | Split hours at each swap, using timed segments |
| P2 | Staff ID format approved (D-011) |
| P3 | Driver ID = the driver's existing employee number, entered by the admin |
| P4 | Staff pick the lot where the vehicle is, or mark it "Not at a lot" |
| P5 | Transfer hours start when staff at the origin lot confirm departure |
| P6 | The admin sets shift start, working days and a grace period |
| P7 | Breaks record a start and an end |
| L1 | English in v1, with layouts ready for right-to-left (Arabic later) |
| R1 | Reports on screen with filters only; no exports |
| A3 | Not provided. To be confirmed before Phase 5 (sizing) |

---

## Owner answers — 2026-11-22 (form 4)

Recorded as D-010. The Staff ID pattern is proposed as D-011.

| ID | Answer |
|---|---|
| N2b | Pick the driver from a list or type the Driver ID. No driver QR cards |
| N2c | Driver fields: photo, Driver ID, full name, mobile, licence number |
| N10 | No restriction on which vehicle types a driver can take |
| N4b–N4d | Attendance for staff only, on their own phones. Events: check in, check out, lunch, tea. Photo and GPS mandatory |
| N5 | Destinations come from an admin-managed list, structured Area → Location |
| N6 | Swapping the vehicle or driver edits the same trip |
| N7, N7b | A cancelled trip ends early and its hours count. The admin decides the vehicle's status |
| N8 | Driver signature at Start only |
| C2b | Staff can end a trip at any lot, including lots they aren't assigned to |
| D2c | During a transfer the vehicle is counted at the origin lot until arrival is confirmed |
| D2d | A transfer records a driver and counts as hours |
| K4 | "Propose one for me" → D-011 |

### Round 1e — follow-ups (asked 2026-11-22)

- **P1 · Critical.** N6 (swaps edit the same trip) conflicts with reporting hours per driver and per vehicle. Proposal: keep one trip, but log each swap as a timed segment so hours split correctly. ★
- **P2 · Important.** Approve the D-011 Staff ID format.
- **P3 · Important.** Driver ID: (a) generated with a `DR` prefix (b) the admin enters the driver's existing employee number, which must be unique ★ (b), since staff type it and drivers already know theirs.
- **P4 · Critical.** Where is a vehicle counted after it is cancelled or swapped out mid-trip? ★ Staff pick the lot, or mark it "not at a lot" so the admin resolves it.
- **P5 · Important.** When do transfer hours start? ★ When the admin creates the transfer; they end when staff confirm arrival.
- **P6 · Important.** Attendance has no shift times, so Late and Absent can't be calculated. (a) Log times only (b) the admin sets shift start and working days, plus a grace period.
- **P7 · Optional.** Do breaks record a start and an end (two taps)? ★ Yes.

Still open: A3 (scale).

---

## Owner answers — 2026-11-18 (form 3)

Recorded as D-004 (palette approved), D-007 (one app), D-008 (.NET 8) and D-009 (business rules).

| ID | Answer |
|---|---|
| K6 | Keep: attendance, modify trip, cancel with reason, driver signature, destination location. Drop: driver QR cards |
| N1 | One app; the role comes from the account |
| N2 | Admins manage the driver list; staff pick or scan the driver (scanning conflicts with dropping driver QR cards → N2b) |
| N3 | Hours reported per staff member and per driver |
| N11 | One active vehicle per staff member at a time |
| C2 | A vehicle can return to any lot; staff choose the lot at End |
| B2 | Staff can start vehicles only from their assigned lots |
| K2 | First reminder after a duration the admin sets, then every 30 minutes; the admin is alerted too |
| B4 | When the admin ends a trip, they enter the actual end time and a reason |
| K1 | Spaces calculated automatically; the admin can set a starting count |
| D1b | A full lot gets a warning but is allowed |
| K3 | Operating hours are information only |
| C6 | Statuses: Available, Active, Inactive, Under maintenance |
| D2 | The admin starts a transfer; staff confirm arrival |
| E2 | Each Type belongs to a Category |
| E3 | QR holds no readable data; image and PDF label downloads; bulk print; manual entry if a scan fails; admins can scan. Damaged sticker → reprint the same code |
| K4 | The system generates Staff IDs (pattern still open) |
| K5 | More admin accounts can be added |
| F2, F2b | The admin resets staff passwords; our team resets the admin's. No forced change at first login |
| F3 | Staff use personal phones |
| F4 | Records are deactivated and history kept |
| G1 | Online only |
| A2 | Single organisation |
| A4 | Out of scope: billing and payments, maintenance management, fuel logs, live vehicle GPS. Confirms AS-01 |
| A5 | No further documents |
| H1 | Stay on .NET 8 (end-of-life risk accepted, D-008) |
| H2 | **Deferred.** The owner will specify the development machine when development starts; planning, UX/UI and documentation come first |
| I1, I2 | Use the estimated hex values; the theme mapping is approved |

Still open: A3 (scale, not given).

### Round 1d — follow-ups (asked 2026-11-18)

- **N2b** Drivers: picked from a list only, or do they also get QR cards to scan? (N2 said "pick or scan", but K6 dropped driver QR cards.)
- **N2c** Which driver fields are needed? The mockup showed photo, ID, name, mobile, email, designation, employee code, licence number and address.
- **N10** Should drivers be limited to the vehicle types they're qualified for?
- **N4b–N4d** Attendance: whose attendance, which events, and is the photo/GPS mandatory? Who sets the shift and break times, and is there a grace period for lateness?
- **N5** Destinations: how is the list managed and structured?
- **N6** Modify trip: start a new trip, or edit the current one?
- **N7, N7b** Cancel trip: does the trip's time count? Does a vehicle-issue cancellation set the vehicle to Under maintenance automatically?
- **N8** Driver signature: at Start only, or at Start and End?
- **C2b** Can staff end a trip at a lot they aren't assigned to?
- **D2c** During a transfer, which lot counts the vehicle until arrival is confirmed?
- **D2d** Should a transfer record a driver and count as hours?
- **K4** What pattern should Staff IDs follow?
- **E2b** What are the real categories and types?

---

## Client clarifications — 2026-11-18

Source: `docs/source/client-clarifications-2026-11-18.md`. Where it differs from the Staff UI mockup, the client response wins.

### Answered by the client

| ID | Client answer | Status |
|---|---|---|
| A1 | Municipal fleet: cars, garbage trucks, ambulances. Staff IDs start with "DM" | Answered |
| A2 | One dedicated admin account; the admin credentials are set up by our team | Inferred: single organisation, needs confirming |
| B1 | One admin role with full access to lots, staff, vehicles and reports | Answered. Number of admin accounts → K5 |
| B3 | The admin account is a dedicated, separate account | Answered: separate accounts |
| B4 | The admin sees all active vehicles in real time and can manually end any session | Partly answered. Editing times and reasons → re-asked |
| C1 | Start = taking the vehicle out of the lot; End = returning it; duration = time outside the lot | Answered |
| C3 | Staff end their own sessions; the admin can end any session | Answered |
| C4 | The session stays open, the app keeps reminding the staff member, and a live timer runs in the Staff app's top bar until the session ends | Answered. Reminder timing → K2 |
| C5 | Start records: staff name and ID, driver name, registration number, category, brand and model, lot, start date and time. End adds the end date and time and the duration | Answered. Signature and destination aren't listed → K6 |
| D1 | Total capacity plus capacity per vehicle type, with occupied and available spaces calculated automatically. Lot type: Open, Underground or Other. Status: Active or Inactive | Answered |
| D3 | Name, code, full address, Google Maps pin, contact person, contact number, opening and closing hours, capacity per type, total capacity, status. Map View of all lots | Answered |
| E1 | Registration number, type, brand, model, colour, photo | Answered |
| F1, F1b | Staff: a unique Staff ID (e.g. `DM324DU2131`) and password issued by the admin. Admin: username or ID and password set up by our team | Answered |
| F2 | The admin issues staff passwords | Partly answered. Forgotten-password flow → re-asked |
| N9 | Staff can only: view their profile, the vehicles they started, lot details and availability across all lots; start; end | Answered |
| AS-02 | Duration = clock time the vehicle spends outside the lot | Confirmed |

### Still open (re-asked 2026-11-18)

A2 (confirm), A3, A4, A5, B2, B4 (editing times), C2, C6, D1b, D2, E2, E3, F2, F3, F4, G1, N1, N2, N3, N11, H1, H2, I1, I2. N4–N8 are folded into K6.

### Round 1c — raised by the client answers

**K1 · Important — Available spaces:** the client lists "Available Parking Spaces" as a field the admin configures, but also says it's calculated automatically. Options: (a) calculated only (b) calculated, but the admin can set a starting count, e.g. spaces taken by vehicles outside the system ★ (b).
**K2 · Critical — Reminders for sessions nobody ended:** when does the first reminder go out, how often does it repeat, and is the admin alerted too? ★ After a duration the admin sets, repeating every 30 minutes, and the admin is alerted at the same time.
**K3 · Important — Operating hours:** (a) shown for information only (b) Start and End are blocked outside those hours. ★ (a).
**K4 · Important — Staff IDs:** (a) the system generates them in a fixed pattern (b) the admin types them in. What does the pattern `DM324DU2131` mean?
**K5 · Important — Admin accounts:** only the one dedicated account, or can more admins be added later?
**K6 · Critical — Mockup features missing from the client's staff list:** attendance, modifying a trip, cancelling a trip with a reason, driver signature, destination location, driver QR cards. Which of these are included?

---

## Staff UI mockup findings — 2026-11-18

Source: `docs/source/staff-ui-mockup.pdf` (31 rough screens). Used as a reference for workflow only, not as the final design.

### Round 1 questions the mockup answers (fully or partly)

| ID | What the mockup shows | Status |
|---|---|---|
| A1 | A municipality fleet: "DM" branding, a "Municipality Parking Lot", Dubai locations. Ford mini trucks are driven by **drivers** from the lot to work locations | Inferred, needs confirming |
| A3 | Dubai, UAE (+971 numbers): one country, one timezone (GST, UTC+4). Counts still unknown | Partly answered |
| B2 | The origin lot is pre-filled on Start, so each staff member has a home lot | Partly answered: one lot or several still open |
| C1 | Start = the vehicle leaves the lot for a selected work location. End = staff confirm "Vehicle received" | Answered |
| C2 | End has no return-lot choice; staff tap "Vehicle received" | Inferred: same lot, needs confirming |
| C5 | Start captures: driver (QR or ID), vehicle (QR or ID), driver signature, origin lot, destination location. End: driver and vehicle scan, then a summary with zone and area. No meter reading, fuel or photo | Answered |
| C6 | Vehicle status "Active" is shown; trips can also be cancelled | Partly answered |
| E1 | Vehicle number (0016), name or model (Ford Mini Truck), plate (D01423 DUBAI), image | Partly answered: required and unique fields still open |
| E3 | Manual ID entry is allowed when a QR can't be scanned. Drivers have QR codes too | Partly answered |
| F1 | Staff: User ID + password, with "Remember me". Admin: same screen, User ID + password | Answered for staff; inferred for admin |

### New items the mockup introduces (not in the requirements PDF)

- **Drivers:** a separate kind of person, not app users. Each has a QR code, ID, photo, mobile, email, designation, employee code, licence number and address.
- **Work locations:** a list of destinations (Dubai Beach, Dubai Park, Museum of the Future, Palm Jumeirah), each with a zone and an area.
- **Modify an active trip:** swap the vehicle and/or the driver, then "Start again".
- **Cancel an active trip** with a reason: driver, vehicle, technical or other issue, plus a written note.
- **Attendance:** check in, check out, lunch and tea break, each recorded with a photo, the time and an automatic GPS location. A schedule shows present, absent and late counts.
- **Staff "Report" tile:** shown on the home screen, but no report screen is designed.
- **Privacy policy link** on the Start and End screens.
- **One login screen with a Staff/Admin toggle:** conflicts with "two dedicated apps" (D-001).

Questions about these are in Round 1b, below Round 1.

---

## Round 1 — asked 2026-11-15

### A. Business context

**A1 · Critical — What vehicles are these, and what does "operating" one mean?**
Why: the PDF uses "running hours", "start vehicle operation" and "parking lots", but never says whose vehicles they are or what work they do. This decides whether "parking" is a customer service or a fleet yard, and what one session means.
Options: (a) company-owned fleet or equipment kept in its own yards or depots (trucks, forklifts, buses, construction machines) (b) rental or hire vehicles (c) customer vehicles parked by staff (valet or commercial car park) (d) other, please describe.
★ No recommendation: only you know. The wording fits (a) best, but that is an assumption.
Answer: _pending_

**A2 · Critical — One organisation, or several client companies with separate data?**
Why: supporting several companies (multi-tenant) changes the data model, logins and admin hierarchy, and is expensive to add later.
Options: (a) single organisation (b) several organisations, data isolated (c) single now, several later.
★ (a) if this is a bespoke build for one client.
Answer: _pending_

**A3 · Important — Scale and geography**
Approximate counts now and in about 3 years: vehicles, lots, staff, admins, Start/End scans per day at peak. One country and timezone, or several?
Why: hosting size, report performance, and how hours that cross midnight or timezones are reported.
Answer: _pending_

**A4 · Critical — Confirm what is out of scope for v1**
None of these appear in the PDF: parking fees, billing or payments; maintenance or servicing; fuel logs; live GPS tracking; customer-facing features. (The web portal is excluded explicitly.)
★ All out of scope for v1.
Answer: _pending_

**A5 · Important — Missing sections**
The PDF's purpose line says it covers "development stages, and project assumptions", but neither section is in it. Is there a fuller version? Please share it if so.
Answer: _pending_

### B. Roles & permissions

**B1 · Critical — Admin levels**
The PDF lists "Admin account management" but defines only one Admin role.
Options: (a) one Admin role with full access (b) Super Admin, who manages admins, plus Admin with full operational access (c) Super Admin plus Lot Admin, limited to specific lots.
★ (b), unless different managers own different lots, in which case (c).
Answer: _pending_

**B2 · Critical — Staff-to-lot assignment** *(gap: the Staff App shows an "assigned parking lot", but Staff Management has no "assign lot" feature)*
Is each staff member assigned to one lot, several, or none? Can staff start or end vehicles parked in a lot they are not assigned to?
Options: (a) one lot, its vehicles only (b) several lots, those vehicles only (c) the assignment is informational and any vehicle is allowed.
★ (b): the admin assigns one or more lots in Staff Management.
Answer: _pending_

**B3 · Important — Can one person be both Admin and Staff?**
Options: (a) no, separate accounts (b) yes, one account with both roles.
★ (a): simpler, and a clearer audit trail.
Answer: _pending_

**B4 · Critical — The admin's role in time records**
§2.8 says the admin "records vehicle start and end times", while the Staff App records them by scan.
Options: (a) admin views only (b) admin can force-end a session and correct times, with a mandatory reason, audited (c) full manual entry and editing.
★ (b).
Answer: _pending_

### C. Core workflow: vehicle Start / End

**C1 · Critical — What happens physically at Start and End?**
Why: this decides the occupancy maths ("occupied vs available") and the vehicle statuses.
Options: (a) Start = the vehicle leaves the lot to work; End = it returns. Its space is free while it is out. (b) The vehicle works inside the lot or site and its space stays occupied. (c) Other.
Answer: _pending_

**C2 · Critical — Where is the vehicle after End?**
Options: (a) always back in its current lot (b) staff select or scan the lot where it was returned; if that lot is different, the location updates and a transfer entry is recorded automatically (c) returning to a different lot is not allowed; an admin must transfer it.
★ (a) if vehicles always return to the same lot; otherwise (b).
Answer: _pending_

**C3 · Critical — Who can end a session, and concurrency**
- End by: (a) only the staff member who started it (b) any staff member assigned to that lot (c) any staff member. Admins can also force-end if B4 = (b).
- Can one staff member have more than one vehicle running at the same time?
★ End by (b); one running vehicle per staff member.
Answer: _pending_

**C4 · Critical — Sessions that are never ended**
Options: (a) the session stays open until someone ends it, and the dashboard flags sessions running longer than a configurable limit (b) auto-close at a fixed limit or at end of day (c) other.
★ (a): auto-closing would invent hours.
Answer: _pending_

**C5 · Critical — What is captured at Start and End?**
The PDF mentions only times. Is anything else needed: hour-meter or odometer reading, fuel level, photo, notes, GPS location, job or purpose reference?
Why: if "running hours" means engine-meter hours rather than clock time, the design changes.
★ Clock time only, recorded by the server, for v1.
Answer: _pending_

**C6 · Critical — Vehicle statuses** *(the PDF mentions "current vehicle status" but never lists the values)*
Proposed: **Available** (parked) · **In Operation** · **Inactive** (set by an admin: out of service or retired). Add **Under Maintenance**? Add **In Transfer** (only if D2 = b)?
Answer: _pending_

### D. Parking lots & transfers

**D1 · Critical — Capacity model**
Options: (a) one capacity number per lot; occupancy = vehicles currently in the lot (b) numbered bays, so a vehicle has a lot and a bay (c) capacity per vehicle type.
If a lot is full, should moving a vehicle into it be blocked, or allowed with a warning?
★ (a); allow with a warning, and show over-capacity on the dashboard.
Answer: _pending_

**D2 · Critical — Transfer process** *(the PDF puts transfers in the Admin App only)*
Options: (a) instant: the admin changes the location and it is done (b) two-step: the admin creates the transfer, then staff scan at the destination to confirm arrival, with the vehicle "In Transfer" in between (c) staff can also start transfers.
Also: is a transfer allowed while the vehicle is In Operation? Is a reason or note required?
★ (a); not allowed while In Operation; optional note.
Answer: _pending_

**D3 · Important — Lot details**
Which fields: name, code, address, GPS pin, contact person, operating hours, notes? Is a map view needed?
★ Name, code, address, optional GPS pin; no map view in v1. **Final (2026-11-27, D-056): no GPS pin and no map view in v1.**
Answer: _pending_

### E. Vehicles, categories & QR codes

**E1 · Critical — Vehicle identification fields**
Which fields are required, and which must be unique: registration or plate number, fleet or asset number, VIN or chassis number, make, model, year, colour, photo? Are documents with expiry dates (insurance, permits) needed?
★ Required: asset number (unique), category, type, current lot. Optional: plate (unique if present), make, model, year, photo. Documents out of scope.
Answer: _pending_

**E2 · Important — How do categories and types relate?**
Options: (a) hierarchical: each type belongs to a category (e.g. Heavy Equipment → Excavator) (b) two independent lists.
★ (a). Please share examples of your real categories and types.
Answer: _pending_

**E3 · Critical — QR code behaviour**
- Content: (a) an opaque random code that only the app can resolve ★ (b) readable vehicle data
- Damaged or lost sticker: (a) issue a new QR and the old one stops working ★ (b) reprint the same code
- Download format: PNG image, printable PDF label (QR + asset number + name), or both ★ both. Is bulk printing needed? (The PDF says "individual".)
- Unreadable QR in the Staff App: allow manual search by asset or plate number? ★ yes, logged as a manual entry
- Admin App scanner: §2.5 lists "identify vehicles through QR scanning". Should admins scan to open vehicle records? ★ yes
Answer: _pending_

### F. Accounts & sign-in

**F1 · Critical — How people sign in**
Options: (a) username or employee ID + password (b) mobile number + SMS OTP (needs a paid SMS provider) (c) email + password.
★ Staff: (a), accounts created by an admin. Admins: (c).
Answer: _pending_

**F2 · Critical — Forgotten passwords** *(the PDF has no recovery flow)*
Options: (a) admins reset staff passwords; admins reset their own by email link (needs an email service) (b) self-service for everyone by email or SMS (c) admin reset only, with the Super Admin resetting admins.
Should staff be forced to change their password on first login?
★ (a); yes to forced change.
Answer: _pending_

**F3 · Important — Devices**
Do staff use personal phones or company phones? Are phones shared between shifts?
Why: shared phones need fast logout, auto-logout and no data left behind between users.
Answer: _pending_

**F4 · Critical — "Delete" vs history**
The PDF allows deleting staff. A hard delete would break hours reports and transfer history.
★ Deactivate instead: the record is hidden from lists, blocked from login or scanning, and its history is kept. Hard delete only for records with no history. The same rule applies to vehicles, lots, categories and types.
Answer: _pending_

### G. Connectivity

**G1 · Critical — Must Start/End work without internet?**
Do all lots have reliable mobile data or Wi-Fi?
Options: (a) online only, with a clear "no connection" message and retry (b) Start/End queued offline and synced later (c) the full app works offline.
Why: offline needs conflict rules (e.g. two staff starting the same vehicle offline) and has to trust device clocks.
★ (a) if connectivity is good; (b) only if there are real dead zones.
Answer: _pending_

### H. Technology & environment

**H1 · Critical — .NET version**
Fact: .NET 8 reached end of support on 10 Nov 2026 and gets no more security patches. .NET 10 is the current LTS release, supported until Nov 2028. Same framework, no architectural change.
★ ASP.NET Core on .NET 10 LTS (D-003).
Answer: _pending_

**H2 · Critical — Development environment**
Fact: this Mac has no .NET SDK, Flutter, Xcode (Command Line Tools only), Android SDK, JDK or Docker. The shell PATH points to Flutter and Android SDK folders that no longer exist.
Will development and builds run on this Mac? Is there an existing SQL Server (company server or Azure) to use, or should SQL Server run locally in Docker? On Apple Silicon it runs under Rosetta emulation, which is fine for development.
★ Install on this Mac: .NET 10 SDK, Flutter stable, Xcode, Android Studio, Docker Desktop + a SQL Server container matching the production version. Nothing will be installed until you approve.
Answer: _pending_

### I. Colour palette

**I1 · Important — Exact hex values**
Only Canary Flash has an exact value (`#FFE014`). Estimates sampled from the screenshots: Ink Heather ≈ `#262A33`, Putty Grey ≈ `#C1BBAF`. Do you have the exact values?
★ Use the estimates if you don't.
Answer: _pending_

**I2 · Important — Light-theme interpretation** *(contradiction: image 2's "Ink Heather" swatch renders as light beige, not ink)*
Proposed:
- Dark theme: Ink Heather backgrounds (60%), Putty Grey for text and secondary elements (30%), Canary accent (10%).
- Light theme: warm off-white backgrounds ≈ `#FBF8F1` (60%), Putty Grey surfaces and borders with Ink text (30%), Canary accent (10%).
Constraint (fact): Canary on a light background is 1.24:1, so in the light theme it can only be a fill behind dark text, never text, icons or thin lines. Status colours get their own muted set in Phase 3.
★ Approve the proposal above.
Answer: _pending_

---

## Round 1b — raised by the Staff UI mockup, asked 2026-11-18

**N1 · Critical — One app or two?** *(the mockup login has a Staff/Admin toggle; the requirements PDF lists two dedicated apps)*
Options: (a) two apps sharing one codebase, as in the PDF's scope (b) one app; the role comes from the account, not a toggle.
★ (a), because the PDF lists two apps as deliverables. In either case the role should come from the account, not a toggle.
Answer: _pending_

**N2 · Critical — Drivers**
Are drivers separate records that admins manage, and who never log in? Are the mockup's fields right (photo, ID, mobile, email, designation, employee code, licence number, address)? Do drivers get printed QR cards, like vehicles?
★ Yes to all.
Answer: _pending_

**N3 · Critical — Whose hours are tracked?** *(the PDF says "hours per staff member"; the mockup shows drivers operating the vehicles)*
Options: (a) per driver, also recording which staff member issued and received the vehicle (b) per staff member only (c) both, reported separately.
★ (a).
Answer: _pending_

**N4 · Critical — Attendance**
Is attendance in scope for v1? Whose attendance: drivers, staff, or both? Is it recorded on the staff member's phone or on each person's own phone? Are the photo and GPS location mandatory? Who sets shift and break times and the rule for being late?
Answer: _pending_

**N5 · Critical — Work locations**
Is the destination list managed by admins? Is the structure Area → Zone → Location? Are work locations kept separate from parking lots?
★ Admins manage it; Area → Location; kept separate from parking lots.
Answer: _pending_

**N6 · Critical — Modifying an active trip ("Start again")**
Options: (a) the current trip closes and a new one starts, so hours are split between them (b) the same trip is edited and its hours stay on one record.
Is a reason required?
★ (a), with a required reason.
Answer: _pending_

**N7 · Critical — Cancelling an active trip**
Options: (a) the trip is voided and its hours don't count (b) the trip ends early and its hours count.
If the reason is "Vehicle issue", should the vehicle be set to Under maintenance automatically?
★ (b); yes, if the Under maintenance status exists (C6).
Answer: _pending_

**N8 · Important — Driver signature**
Options: (a) at Start only, as in the mockup (b) at Start and End.
★ (a).
Answer: _pending_

**N9 · Important — Staff "Report" tile**
What should staff see? Today's trips, the trips they issued or received, driver hours, an attendance summary?
Answer: _pending_

**N10 · Important — Driver eligibility**
The mockup shows the designation "Mini Truck Driver". Should drivers be limited to the vehicle types they're qualified for?
Answer: _pending_

**N11 · Critical — One active trip per driver and per vehicle?**
★ Yes, both. (This replaces C3b.)
Answer: _pending_

---

## Round 2 — next, not asked yet

Reports and dashboard (filters, date ranges, export to PDF/Excel/CSV, share) · notifications (push for long-running vehicles or full lots) · audit log and data retention · languages · time display format and timezone · hosting (Azure / AWS / on-prem) and SQL Server edition and licensing · app-store distribution (public vs private) and developer accounts · app names, logo, typography · minimum OS versions and tablet support · backup and recovery targets · data-protection obligations · performance targets.

---

## Gaps & contradictions found in the PDF

| # | Finding | Question |
|---|---|---|
| G-01 | Staff App shows an "assigned parking lot", but Staff Management has no assignment feature | B2 |
| G-02 | §2.8 says the admin "records" start and end times, while recording happens in the Staff App | B4 |
| G-03 | The purpose line mentions "development stages" and "project assumptions", which are absent | A5 |
| G-04 | "Delete staff" conflicts with keeping activity history and reports | F4 |
| G-05 | The link between Start/End and lot occupancy is undefined | C1, C2 |
| G-06 | "Current vehicle status" is used but its values are never defined | C6 |
| G-07 | Transfer is admin-only; how the physical move happens is not addressed | D2 |
| G-08 | "Admin account management" exists, but there is no admin hierarchy | B1 |
| G-09 | QR scanning is listed under Admin QR management; whether admins scan is unclear | E3 |
| G-10 | No password recovery, offline behaviour, notifications or report-export requirements | F2, G1, Round 2 |
| G-11 | Palette image 2 labels a light swatch "Ink Heather"; only one exact hex is given | I1, I2 |
| G-12 | The mockup login has a Staff/Admin toggle; the PDF says two dedicated apps | N1 |
| G-13 | Drivers appear in the mockup but not in the requirements PDF | N2, N3 |
| G-14 | An attendance module appears in the mockup but not in the requirements PDF | N4 |
| G-15 | The PDF tracks "hours per staff member"; the mockup shows drivers operating vehicles | N3 |
| G-16 | Mockup sample data contradicts itself: the End summary shows 10:20 → 12:20 as 03:20:12 and as "2 h 20 min" | Treated as placeholder data |
| G-17 | The client lists "Available Parking Spaces" as a configured field and also as auto-calculated | K1 |
| G-18 | The client's staff function list leaves out mockup features (attendance, modify, cancel, signature, destinations) | K6 |
| G-19 | The client says "Vehicle Category" in §3 and "Vehicle Type" in §6; the original PDF has both | E2 |
| G-20 | Drivers appear only as "Driver Name"; there is no driver management in the client's list | N2 |
| G-21 | A single live timer in the top bar implies one active vehicle per staff member | N11 |

## Assumptions register (all unconfirmed)

| ID | Assumption | Linked |
|---|---|---|
| AS-01 | No payments, billing or fees in v1 | A4 |
| AS-02 | Operating hours = clock time between Start and End | C5 |
| AS-03 | Start and End times come from the server clock, not the device | C5, G1 |
| AS-04 | A vehicle is in exactly one lot at any moment | C1, D2 |
| AS-05 | Each vehicle has exactly one active QR code at a time | E3 |
