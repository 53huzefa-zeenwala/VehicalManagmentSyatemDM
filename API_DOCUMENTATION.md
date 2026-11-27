# API Documentation

**Version:** 1.0 · **Date:** 2026-11-27 · **Status:** All milestones implemented (M1–M5) and tested (105 tests); M6 release files in place. This file is the contract for the Flutter app (Phase 8).
**Related:** conventions and error design in `ARCHITECTURE.md` (§4–§7, §11); tables in `DATABASE_DESIGN.md`.

---

## 1. Basics

| Topic | Rule |
|---|---|
| Base URL | `https://<server>:<port>/api/v1` (the address lives in the app's build config, not in this public repo) |
| HTTPS | Self-signed certificate; the app accepts it (D-036) |
| Format | JSON, UTF-8, camelCase. Send `Content-Type: application/json` (uploads: `multipart/form-data`) |
| App version header | `X-App-Version: 1.0.0` on every request (stored with error logs) |
| Auth | Cookie `vms.auth`, set by `POST auth/login`, sent automatically by the app's cookie jar. HttpOnly, Secure, SameSite=Strict |
| Times | ISO-8601 UTC, e.g. `2026-11-26T08:15:00.000Z`. The app shows GST |
| Enums | Strings, e.g. `"Available"` |
| IDs | Integers. Vehicles are also found by `qrToken` (22 chars) or registration number |
| Lists | `?page=1&pageSize=20` (max 100) → `{ "items": [...], "page": 1, "pageSize": 20, "totalCount": 134 }` |
| Files | Responses carry URLs like `"/api/v1/files/vehicles/3f2a…1b.jpg"`; fetch with the cookie (§4.14) |
| Refresh | No push or auto-refresh (D-035); the app reloads on screen open and pull to refresh |
| Body size | 1 MB, except photo uploads (21 MB, photos ≤ 20 MB) |
| Optimistic concurrency | Records several admins can edit (vehicles, lots, staff, admins, settings) return `rowVersion` (base64). Every `PUT` on them must send it back; if the record changed in between → `409 CONCURRENCY_CONFLICT` (reload and retry) |
| Inactive records | Lists hide inactive categories, types, areas, locations and lots unless an admin passes `?includeInactive=true`; staff never see them |

**Roles:** `A` = admin, `S` = staff, `A,S` = both, `—` = anonymous.

## 2. Errors

Every non-2xx response is `application/problem+json`:

```json
{
  "status": 409,
  "code": "VEHICLE_NOT_AVAILABLE",
  "title": "Vehicle not available",
  "detail": "Vehicle D 01423 is on an active trip and can't be started.",
  "errors": null,
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

The app shows `detail`, branches on `code`, highlights fields from `errors`, and shows `traceId` as "Ref". Unexpected errors in Production say "Something went wrong on the server. Ref: …" (D-033).

**General codes** (ARCHITECTURE §5.2): `REQUEST_FAILED` (any other 4xx) · `ID_SEQUENCE_EXHAUSTED` 409 · `VALIDATION_FAILED` 400 · `MALFORMED_REQUEST` 400 · `INVALID_FILE` 400 · `UNAUTHENTICATED` 401 · `INVALID_CREDENTIALS` 401 · `ACCOUNT_LOCKED` 401 · `ACCOUNT_DEACTIVATED` 401 · `FORBIDDEN` 403 · `NOT_FOUND` 404 · `ROUTE_NOT_FOUND` 404 · `FILE_NOT_FOUND` 404 · `METHOD_NOT_ALLOWED` 405 · `DUPLICATE_VALUE` 409 · `CONCURRENCY_CONFLICT` 409 · `PAYLOAD_TOO_LARGE` 413 · `UNSUPPORTED_MEDIA_TYPE` 415 · `TOO_MANY_REQUESTS` 429 · `INTERNAL_ERROR` 500 · `DATABASE_SCHEMA_ERROR` 500 · `DATABASE_UNAVAILABLE` 503 · `DATABASE_NOT_READY` 503 · `DATABASE_TIMEOUT` 503.

**Business codes** (all 409 unless noted):

| Code | When |
|---|---|
| `VEHICLE_NOT_FOUND` (404) | Unknown QR token or registration (QR-06) |
| `VEHICLE_NOT_AVAILABLE` | Vehicle Active, Inactive or Under maintenance |
| `VEHICLE_IN_TRANSFER` | Vehicle has an open transfer |
| `VEHICLE_NOT_AT_LOT` | Vehicle isn't located at the origin lot (BR-04) |
| `VEHICLE_MISMATCH` | End: scanned vehicle isn't the trip's current vehicle |
| `STAFF_HAS_ACTIVE_TRIP` | Staff already on a trip or driving a transfer (BR-02, D-026) |
| `LOT_INACTIVE` | Inactive lot used for start, return or transfer (LOT-07) |
| `TRIP_NOT_ACTIVE` | Action on an ended or cancelled trip |
| `NOT_YOUR_TRIP` (403) | Staff acting on someone else's trip |
| `HANDOVER_TARGET_INVALID` | Target is inactive, not staff, yourself, or busy |
| `TRANSFER_INVALID_STATE` | Depart/arrive/cancel in the wrong state |
| `CANNOT_DEACTIVATE_SELF` | Admin deactivating their own account |
| `STATUS_CHANGE_NOT_ALLOWED` | Inactive / maintenance while the vehicle is Active |
| `INVALID_ID_PREFIX` (400) | Staff prefix `ADM` or not letters |

**Unknown routes:** every endpoint needs a session unless marked otherwise (`401 UNAUTHENTICATED` when signed out). A URL that matches no endpoint answers `404 ROUTE_NOT_FOUND` to everyone, and a wrong method `405 METHOD_NOT_ALLOWED` (D-044).

**Passwords:** 8–128 characters (D-044).

Warnings that don't block (full lot, BR-09) come back in successful responses as `"warnings": [{ "code": "LOT_FULL", "message": "…" }]`.

## 3. Shared shapes

```jsonc
// VehicleCard
{ "id": 12, "registrationNumber": "D 01423", "category": "Truck", "type": "Garbage truck",
  "brand": "Isuzu", "model": "NPR", "colour": "White", "photoUrl": "/api/v1/files/vehicles/….jpg",
  "status": "Available", "currentLot": { "id": 3, "name": "Al Qusais Depot" } /* null = not at a lot */ }

// Lot (list, map and detail)
{ "id": 3, "code": "AQD", "name": "Al Qusais Depot", "address": "…", "latitude": 25.27, "longitude": 55.38,
  "contactName": "…", "contactNumber": "+971…", "opensAt": "06:00", "closesAt": "22:00",
  "lotType": "Open", "isActive": true, "baselineOccupied": 5,
  "total": { "capacity": 80, "occupied": 61, "available": 19, "isFull": false, "isOverCapacity": false },
  "byType": [ { "vehicleTypeId": 2, "type": "Garbage truck", "category": "Truck", "capacity": 20,
                "baselineOccupied": 0, "occupied": 22, "available": -2 },
              { "vehicleTypeId": 5, "type": "Sedan", "category": "Car", "capacity": null,
                "baselineOccupied": 0, "occupied": 3, "available": null } ],   // no capacity set for this type
  "rowVersion": "…" }

// StaffRef
{ "id": 7, "loginId": "DM2600042", "fullName": "Imran Tahir", "photoUrl": null }
```

## 4. Endpoints

### 4.1 Auth (M1)

| Method | Path | Role | Purpose |
|---|---|---|---|
| POST | `auth/login` | — | Sign in; sets the cookie |
| POST | `auth/logout` | A,S | Ends the session; clears the cookie |
| GET | `auth/me` | A,S | Current user, role, staff stats |
| POST | `auth/change-password` | A,S | Own password; ends other sessions |

```jsonc
// POST auth/login
{ "loginId": "dm2600042", "password": "•••", "rememberMe": true }
// 200
{ "id": 7, "loginId": "DM2600042", "role": "Staff", "fullName": "Imran Tahir" }
// 401 INVALID_CREDENTIALS | ACCOUNT_LOCKED (detail says until when) | ACCOUNT_DEACTIVATED; 429 TOO_MANY_REQUESTS

// Implemented in M1: login and GET auth/me return
{ "id": 7, "loginId": "DM2600042", "role": "Staff", "fullName": "Imran Tahir",
  "mobile": "+971…", "email": null, "licenceNumber": "…", "photoUrl": null }
// M3 adds, for staff: "hasActiveTrip": true, "thisMonth": { "trips": 18, "totalSeconds": 149400 }
//   (GST calendar month; trips taken part in, and their own driving time). Admins: false / null.

// POST auth/change-password
{ "currentPassword": "•••", "newPassword": "•••" }   // 204
```

### 4.2 Vehicle lookup (staff, M3 ✅)

| Method | Path | Role | Purpose |
|---|---|---|---|
| GET | `vehicles/lookup?qr={token}` or `?registration={text}` | S | Vehicle card + whether it can be started from my lot |

```jsonc
// 200
{ "vehicle": { "id": 12, "registrationNumber": "D 01423", "category": "Truck", "type": "Garbage truck",
               "brand": "Isuzu", "model": "NPR", "colour": "White", "photoUrl": null, "status": "Available",
               "currentLot": { "id": 3, "code": "AQD", "name": "Al Qusais Depot" } },
  "canStart": false,
  "blockReason": { "code": "VEHICLE_NOT_AVAILABLE", "message": "D 01423 is under maintenance." } }
```
- Send exactly one of `qr` or `registration` (registration ignores case, spaces and dashes).
- `blockReason.code`: `VEHICLE_NOT_AVAILABLE` (out, under maintenance or inactive), `VEHICLE_NOT_AT_LOT`, `LOT_INACTIVE`, `STAFF_HAS_ACTIVE_TRIP`., `VEHICLE_IN_TRANSFER` (waiting for or on a transfer). Staff can start from any active lot (D-047).
- 404 `VEHICLE_NOT_FOUND` for unknown codes. Admins get 403 (no scanning, D-021). Used by start, end (match check), swap and transfer arrival.

### 4.3 Vehicles (admin, M2)

| Method | Path | Role | Purpose |
|---|---|---|---|
| GET | `vehicles?search=&status=&categoryId=&typeId=&lotId=&notAtLot=true&page=&pageSize=` | A | List. Search matches registration (ignoring case, spaces and dashes), brand, model |
| GET | `vehicles/{id}` | A | Detail with QR token |
| POST | `vehicles` | A | Create (QR token generated) |
| PUT | `vehicles/{id}` | A | Edit details |
| PUT | `vehicles/{id}/status` | A | `{ "status": "UnderMaintenance", "rowVersion" }` (Available, Inactive, UnderMaintenance). `409 STATUS_CHANGE_NOT_ALLOWED` while on a trip or transfer (D-043) |
| PUT | `vehicles/{id}/location` | A | `{ "lotId": 3, "rowVersion" }`: place at an active lot, e.g. to resolve "Not at a lot". `409 VEHICLE_NOT_AVAILABLE` while on a trip or transfer |
| PUT | `vehicles/{id}/photo` | A | Multipart `file`, ≤ 20 MB, JPEG/PNG |
| GET | `vehicles/{id}/history?page=&pageSize=` | A | Trips, transfers and admin changes, newest first (see §4.13) |
| GET | `vehicles/qr-labels?ids=1&ids=2&ids=3` | A | Label data for the app's PDF (D-040); 1–500 vehicles |

```jsonc
// POST vehicles   (lotId null = "Not at a lot"; status optional: Available | Inactive | UnderMaintenance)
{ "registrationNumber": "D 01423", "vehicleTypeId": 2, "brand": "Isuzu", "model": "NPR",
  "colour": "White", "lotId": 3, "status": "Available" }
// 201 → Vehicle
{ "id": 12, "registrationNumber": "D 01423", "categoryId": 1, "category": "Truck", "vehicleTypeId": 2,
  "type": "Garbage truck", "brand": "Isuzu", "model": "NPR", "colour": "White", "photoUrl": null,
  "status": "Available", "currentLot": { "id": 3, "code": "AQD", "name": "Al Qusais Depot" },
  "isNotAtLot": false, "qrToken": "q8Jd0…", "createdAtUtc": "…", "updatedAtUtc": "…", "rowVersion": "AAAAAAAAB9E=" }
// 409 DUPLICATE_VALUE (registrationNumber); 400 for an unknown or inactive type, or an inactive lot
// PUT vehicles/{id}: same fields without lotId and status, plus rowVersion

// GET vehicles/qr-labels → [ { "vehicleId": 1, "qrToken": "…", "registrationNumber": "D 01423", "category": "Truck", "type": "Garbage truck" } ]
```

### 4.4 Vehicle categories and types (M2)

| Method | Path | Role | Purpose |
|---|---|---|---|
| GET | `vehicle-categories?includeInactive=` | A,S | Categories with their types |
| POST | `vehicle-categories` | A | `{ "name": "Truck" }` |
| PUT | `vehicle-categories/{id}` | A | `{ "name": "…", "isActive": true }` |
| POST | `vehicle-categories/{id}/types` | A | `{ "name": "Garbage truck" }` |
| PUT | `vehicle-types/{id}` | A | `{ "name": "…", "isActive": true }` |

### 4.5 Parking lots (M2)

| Method | Path | Role | Purpose |
|---|---|---|---|
| GET | `lots?includeInactive=` | A,S | `LotSummary[]` with occupancy (map + list) |
| GET | `lots/{id}` | A,S | Same shape as a list item (staff get 404 for inactive lots) |
| POST | `lots` | A | Create |
| PUT | `lots/{id}` | A | Edit, including capacities and baselines (`typeCapacities` replaces the whole list), plus `rowVersion` |
| PUT | `lots/{id}/status` | A | `{ "isActive": false, "rowVersion" }` |

```jsonc
// POST lots
{ "code": "AQD", "name": "Al Qusais Depot", "address": "…", "latitude": 25.27, "longitude": 55.38,
  "contactName": "…", "contactNumber": "+971…", "opensAt": "06:00", "closesAt": "22:00",
  "lotType": "Open", "totalCapacity": 80, "baselineOccupied": 5,
  "typeCapacities": [ { "vehicleTypeId": 2, "capacity": 20, "baselineOccupied": 0 } ] }
```

### 4.6 Destinations (M2)

| Method | Path | Role | Purpose |
|---|---|---|---|
| GET | `areas?includeInactive=` | A,S | Areas with their locations |
| POST | `areas` | A | `{ "name": "Deira" }` |
| PUT | `areas/{id}` | A | `{ "name": "…", "isActive": true }` |
| POST | `areas/{id}/locations` | A | `{ "name": "Fish Market" }` |
| PUT | `locations/{id}` | A | `{ "name": "…", "isActive": true }` |

### 4.7 Staff (M2)

| Method | Path | Role | Purpose |
|---|---|---|---|
| GET | `staff?search=&isActive=&page=&pageSize=` | A | List. Search matches name, Staff ID, mobile |
| GET | `staff/{id}` | A | Detail |
| POST | `staff` | A | Create; returns the generated Staff ID |
| PUT | `staff/{id}` | A | Edit name, mobile, email, licence number, plus `rowVersion` (staff have no lots, D-047) |
| PUT | `staff/{id}/status` | A | `{ "isActive": false, "rowVersion" }`; ends sessions at once (AUTH-07) |
| POST | `staff/{id}/reset-password` | A | `{ "newPassword": "…" }` (8–128 characters) → 204; unlocks the account and ends its sessions |
| PUT | `staff/{id}/photo` | A | Multipart, ≤ 20 MB |
| GET | `staff/{id}/trips?from=&to=&page=` | A | Trips and hours (STF-03), see §4.9 |
| GET | `staff/handover-candidates?search=` | S | Active staff with no active trip → `StaffRef[]` (transfer drivers excluded, D-026), see §4.9 |

```jsonc
// POST staff
{ "fullName": "Imran Tahir", "mobile": "+971…", "email": null, "licenceNumber": "…",
  "password": "•••" }
// 201 → Staff
{ "id": 7, "loginId": "DM2600042", "fullName": "Imran Tahir", "mobile": "+971…", "email": null,
  "licenceNumber": "…", "photoUrl": null, "isActive": true, "createdAtUtc": "…", "rowVersion": "…" }
```

### 4.8 Admins (M2)

| Method | Path | Role | Purpose |
|---|---|---|---|
| GET | `admins` | A | List |
| GET | `admins/{id}` | A | One admin (for the edit form) |
| POST | `admins` | A | `{ "fullName", "mobile", "email", "password" }` → `ADM2600002` |
| PUT | `admins/{id}` | A | Edit name, mobile, email, plus `rowVersion` |
| PUT | `admins/{id}/status` | A | `{ "isActive", "rowVersion" }`; not yourself (`CANNOT_DEACTIVATE_SELF`) |
| POST | `admins/{id}/reset-password` | A | AUTH-05 |

### 4.9 Trips (M3 ✅)

| Method | Path | Role | Purpose |
|---|---|---|---|
| POST | `trips` | S | Start (TRP-01) |
| GET | `trips/active` | S | My active trip, or `204` when none |
| GET | `trips?from=&to=&status=&staffId=&vehicleId=&lotId=&page=&pageSize=` | A,S | Staff: trips they took part in (including handed-over ones). Admin: all, with filters. `from`/`to` are GST dates of the trip start; `lotId` = origin lot; newest first |
| GET | `trips/{id}` | A,S | Detail: segments, signature URL (reminder log added in M5). Staff: only trips they took part in, otherwise 404 |
| GET | `trips/live?lotId=&typeId=` | A | Active trips, overdue first, then oldest first (TRP-05). Overdue = the first reminder time has passed |
| POST | `trips/{id}/swap-vehicle` | S | Change vehicle (TRP-07) |
| POST | `trips/{id}/handover` | S | Hand over to another staff (TRP-07, D-041) |
| POST | `trips/{id}/cancel` | S | Cancel (TRP-08) |
| POST | `trips/{id}/end` | S | End (TRP-09) → summary |

```jsonc
// POST trips   (the origin is the active lot the vehicle is parked at; staff have no assigned lots, D-047)
{ "vehicleQrToken": "q8Jd0…",   // or "vehicleRegistration": "D 01423"
  "destinationLocationId": 41, "signaturePngBase64": "iVBORw0KGgo…" }   // "data:image/png;base64,…" also accepted
// 201 → TripDetail. 409 VEHICLE_NOT_AVAILABLE | VEHICLE_NOT_AT_LOT | STAFF_HAS_ACTIVE_TRIP |
//       LOT_INACTIVE | VEHICLE_IN_TRANSFER; 404 VEHICLE_NOT_FOUND;
//       400 INVALID_FILE (signature not a PNG or over 512 KB), VALIDATION_FAILED (inactive destination)

// TripDetail
{ "id": 501, "status": "Active", "startedAtUtc": "…", "endedAtUtc": null, "totalSeconds": null,
  "isOverdue": false, "nextReminderAtUtc": "…",
  "originLot": { "id": 3, "code": "AQD", "name": "…" },
  "destination": { "locationId": 41, "area": "Deira", "location": "Fish Market" },
  "currentStaff": { "id": 7, "loginId": "DM2600042", "fullName": "…", "photoUrl": null },
  "currentVehicle": { /* vehicle card, as in §4.2 */ },
  "signatureUrl": "/api/v1/files/signatures/….png",
  "segments": [ { "seq": 1, "kind": "Start", "staff": { /* StaffRef */ },
                  "vehicle": { "id": 12, "registrationNumber": "D 01423", "category": "Truck", "type": "Garbage truck" },
                  "startedAtUtc": "…", "endedAtUtc": null, "reason": null,
                  "outgoingVehicleLot": null, "outgoingVehicleNotAtLot": false } ],
  "returnLot": null, "cancel": null, "warnings": [] }
// The app runs the timer from startedAtUtc. TripSummary (lists) = id, status, times, totalSeconds, isOverdue,
// originLot, returnLot, destination, currentStaff, currentVehicle, segmentCount.

// POST trips/{id}/swap-vehicle   (or "newVehicleRegistration"). The new vehicle must be Available at any active lot;
// the old one becomes Available at oldVehicleLotId (an active lot) or Not at a lot (D-043).
{ "newVehicleQrToken": "…", "reason": "Flat tyre", "oldVehicleLotId": 3 }   // null = Not at a lot

// POST trips/{id}/handover   (target: an active staff member with no active trip; pick from staff/handover-candidates)
{ "toStaffId": 9, "reason": "Shift change" }
// 409 HANDOVER_TARGET_INVALID. Afterwards the previous driver gets 403 NOT_YOUR_TRIP for actions but keeps the trip in history.

// POST trips/{id}/cancel
{ "category": "Technical", "note": "Engine warning light", "vehicleLotId": null }
// The vehicle (and the old vehicle on swap-vehicle) becomes Available at that lot or Not at a lot;
// admins decide its status (D-043). The admin notification is sent from M5 (notifications). Note is required.

// POST trips/{id}/end
{ "vehicleQrToken": "…", "returnLotId": 3 }
// 200 → TripDetail with returnLot, endedAtUtc, totalSeconds, and
//   "warnings": [ { "code": "LOT_FULL", "message": "Al Qusais Depot is over capacity (61 of 60). The vehicle was still returned." } ]
// 409 VEHICLE_MISMATCH | TRIP_NOT_ACTIVE | LOT_INACTIVE; 403 NOT_YOUR_TRIP; 404 if not your trip at all
```

**Also in M3:**

| Method | Path | Role | Purpose |
|---|---|---|---|
| GET | `staff/handover-candidates?search=` | S | Up to 50 active staff (not you) with no active trip → `StaffRef[]` |
| GET | `staff/{id}/trips?from=&to=&page=&pageSize=` | A | `{ "staff": StaffRef, "totalSeconds": 5400, "trips": Page<TripSummary> }`. `totalSeconds` = this staff member's segment time inside the GST date range (BR-06) |

```jsonc
// Concurrency: two phones acting on the same vehicle, staff member or trip at the same moment are serialised;
// the loser gets the normal rule error (e.g. 409 VEHICLE_NOT_AVAILABLE), or 503 DATABASE_TIMEOUT if it waited over 10 s.
```

### 4.10 Transfers (M4 ✅)

| Method | Path | Role | Purpose |
|---|---|---|---|
| POST | `transfers` | A | `{ "vehicleId", "toLotId", "note" }` → 201 Transfer (TRF-01). All active staff get a "transfer waiting" notification |
| GET | `transfers?status=&lotId=&vehicleId=&from=&to=&page=&pageSize=` | A | List, newest first. `lotId` = from or to that lot; `from`/`to` = GST creation dates |
| GET | `transfers/{id}` | A,S | Detail with timeline |
| POST | `transfers/{id}/cancel` | A | Only while `PendingDeparture` (TRF-06) |
| GET | `transfers/tasks` | S | `{ "awaitingDeparture": [Transfer], "inTransit": [Transfer] }`: every open transfer (any staff may act, D-047) |
| GET | `transfers/by-vehicle?qr={token}` | S | The open transfer of a scanned vehicle (e.g. at arrival) |
| POST | `transfers/{id}/depart` | S | I confirm departure and **drive** it (TRF-02, D-020). Vehicle becomes Active but still counts at the origin lot (TRF-03). Other staff are notified |
| POST | `transfers/{id}/arrive` | S | Any staff member confirms arrival (TRF-04) → vehicle Available at the destination; `warnings` (LOT_FULL) as for trip end |

```jsonc
// Transfer
{ "id": 9, "status": "InTransit",            // PendingDeparture | InTransit | Completed | Cancelled
  "vehicle": { "id": 12, "registrationNumber": "D 01423", "category": "Truck", "type": "Garbage truck" },
  "fromLot": { "id": 3, "code": "AQD", "name": "…" }, "toLot": { "id": 4, "code": "JAY", "name": "…" },
  "note": "Rebalance", "createdBy": { /* person */ }, "createdAtUtc": "…",
  "driver": { "id": 7, "loginId": "DM2600042", "fullName": "…", "photoUrl": null }, "departedAtUtc": "…",
  "arrivedBy": null, "arrivedAtUtc": null, "cancelledBy": null, "cancelledAtUtc": null,
  "drivingSeconds": 1260, "isOverdue": false, "warnings": [] }
```

Errors:
- `VEHICLE_NOT_AVAILABLE`: out, under maintenance or inactive (VEH-03).
- `VEHICLE_NOT_AT_LOT`: not at a lot, or moved since the transfer was created.
- `VEHICLE_IN_TRANSFER`: already has an open transfer.
- `LOT_INACTIVE`: inactive destination at creation.
- `TRANSFER_INVALID_STATE`: wrong step, e.g. departing twice or cancelling after departure.
- `STAFF_HAS_ACTIVE_TRIP`: the staff member is on a trip or already driving a transfer (D-026).
- 400 `VALIDATION_FAILED`: destination equals origin.

A vehicle with an open transfer can't start a trip (`VEHICLE_IN_TRANSFER`), and admins can't move it by hand.

### 4.11 Notifications (M5 ✅, D-039)

| Method | Path | Role | Purpose |
|---|---|---|---|
| GET | `notifications?unreadOnly=&page=&pageSize=` | A,S | `{ "items": [...], "page", "pageSize", "totalCount", "unreadCount" }`, newest first |
| POST | `notifications/{id}/read` | A,S | Mark one read → 204 (404 if not yours) |
| POST | `notifications/read-all` | A,S | Mark all read → 204 |

```jsonc
// Notification
{ "id": 120, "type": "TripReminder", "title": "Trip still running: D 01423",
  "body": "D 01423 has been out for 2 h 5 min. End the trip when the vehicle is back.",
  "targetType": "Trip", "targetId": 501, "createdAtUtc": "…", "readAtUtc": null }
```

| Type | Sent to | When |
|---|---|---|
| `TripReminder` / `TripOverdue` | Driver / all active admins | First reminder after the delay setting, then every 30 min (TRP-06) |
| `TransferReminder` / `TransferOverdue` | Driver / all active admins | Same rule while in transit (TRF-07) |
| `TransferAwaitingDeparture` | All active staff | Transfer created |
| `TransferAwaitingArrival` | All active staff except the driver | Departure confirmed |
| `TripCancelled` | All active admins | Trip cancelled; the admin decides the vehicle's status (D-043) |
| `VehicleReleased` | All active admins | Vehicle swapped out of a trip (D-043) |

Push to phones is off until a Firebase project exists (D-039). A background job checks reminders every minute; each reminder is sent once.

### 4.12 Dashboard and reports (M5 ✅, admin)

| Method | Path | Purpose |
|---|---|---|
| GET | `dashboard` | A-01, RPT-01 (below) |
| GET | `reports/vehicle-hours?from=&to=&lotId=&categoryId=&typeId=&vehicleId=` | R-1: per vehicle, trip and transfer seconds, total, and per GST day. `lotId` = trips started from / transfers leaving that lot |
| GET | `reports/staff-hours?from=&to=&staffId=` | R-2: per staff member (= driver hours): trips, transfers, trip and transfer seconds, total, per GST day |
| GET | `reports/lot-utilization?from=&to=&lotId=` | R-3: current occupancy per lot and type, plus trips started, trips returned, transfers out and in during the period (default: today) |
| GET | `reports/trip-log?from=&to=&status=&staffId=&vehicleId=&lotId=&page=` | R-4: paged trip list, including cancelled and handed-over trips |
| GET | `reports/transfers?status=&lotId=&vehicleId=&from=&to=&page=` | R-5: paged transfer list |

- `from`/`to` are GST dates (`2026-11-26`), both inclusive. They are required for R-1 and R-2, and a range can be at most 366 days.
- Hours crossing midnight are split by GST day (RPT-04).
- Running trips and transfers count up to now.
- No exports (RPT-03).

```jsonc
// GET dashboard
{ "onTripNow": 14, "transfersInTransit": 2, "tripsToday": 63, "secondsOutToday": 151200, "freeSpaces": 212,
  "tripsPerHourToday": [ { "hour": 0, "trips": 0 }, … 24 entries (GST hours) ],
  "needsAttention": {
    "overdueTrips": 3, "overdueTripList": [ /* TripSummary, up to 20 */ ], "overdueTransfers": 0,
    "vehiclesNotAtLot": 1, "vehiclesNotAtLotList": [ /* VehicleRef, up to 20 */ ],
    "lotsOverCapacity": [ { "lot": { "id", "code", "name" }, "capacity": 60, "occupied": 61 } ],
    "transfersWaiting": 2, "transfersWaitingList": [ /* Transfer, up to 20 */ ] } }

// GET reports/staff-hours
{ "totalSeconds": 7200, "staff": 1,
  "rows": [ { "staff": { /* StaffRef */ }, "trips": 1, "transfers": 0, "tripSeconds": 7200, "transferSeconds": 0,
              "totalSeconds": 7200, "days": [ { "date": "2026-11-14", "seconds": 3600 }, { "date": "2026-11-15", "seconds": 3600 } ] } ] }
```

### 4.13 Settings and audit (admin)

| Method | Path | Purpose |
|---|---|---|
| GET | `settings` | `{ "staffIdPrefix": "DM", "reminderDelayMinutes": 120, "reminderRepeatMinutes": 30, "rowVersion" }` |
| PUT | `settings` | `{ "staffIdPrefix": "DM", "reminderDelayMinutes": 120, "rowVersion" }`. Prefix 1–5 letters, not `ADM` (`INVALID_ID_PREFIX`); delay 1–1,440 minutes; repeat fixed at 30 (D-045) |
| GET | `audit-logs?userId=&entityType=&entityId=&from=&to=&page=` | AUD-02 ✅: `{ items: [ { "id", "atUtc", "user": { "id", "loginId", "fullName" }, "action", "entityType", "entityId", "before": { … }, "after": { … } } ], … }`. Actions: Create, Update, Delete, Deactivate, Reactivate, StatusChange, LocationResolve, PasswordReset, PasswordChange |

**Also in M4:** `GET vehicles/{id}/history?page=&pageSize=` (A, VEH-05): newest first, entries `{ "atUtc", "endedAtUtc", "kind": "Trip" | "Transfer" | <audit action>, "description", "durationSeconds", "tripId", "transferId" }`.

**Trip detail (M5):** `GET trips/{id}` now carries `"reminders": [ { "number": 1, "sentAtUtc": "…" } ]` for admins (empty for staff).

### 4.14 Files and health

| Method | Path | Role | Purpose |
|---|---|---|---|
| GET | `files/{kind}/{name}` | A,S | `kind` = `vehicles` \| `staff` \| `signatures`. Permission rules in ARCHITECTURE §11: signatures for admins and for staff on trips they took part in. `Cache-Control: private, max-age=31536000, immutable` |
| GET | `/health` | — | `{ "status": "Healthy", "database": "Healthy" }` (outside `/api/v1`) |

## 5. Change log

| Version | Date | Change |
|---|---|---|
| 0.1 | 2026-11-26 | First contract draft (M0) |
| 0.2 | 2026-11-26 | Approved; cancel / swap-out vehicle rule (D-043) |
| 1.0 | 2026-11-27 | M4 transfers, M5 notifications/reminders/dashboard/reports/audit log, vehicle history; `VEHICLE_IN_TRANSFER`, `TRANSFER_INVALID_STATE`; all sections implemented |
| 0.6 | 2026-11-27 | D-047: staff have no assigned lots. Removed `lotIds`/`lots` from staff, `assignedLots` from `auth/me`, `lotId` filter from the staff list, `originLotId` from start, codes `LOT_NOT_ASSIGNED` and `NOT_ORIGIN_LOT_STAFF` |
| 0.5 | 2026-11-27 | M3 implemented: vehicle lookup, trips (start, active, swap, hand-over, cancel, end, history, detail, live), staff trips and hours, hand-over picker; `auth/me` adds `hasActiveTrip`, `thisMonth`; staff can view signatures of their trips |
| 0.4 | 2026-11-27 | M2 implemented: categories and types, vehicles, QR labels, lots with occupancy, destinations, staff, admins, settings; `rowVersion` on edits; `auth/me` adds `assignedLots` |
| 0.3 | 2026-11-27 | M1 implemented: auth, files, health; `REQUEST_FAILED`, `ID_SEQUENCE_EXHAUSTED`; unknown-route and password rules (D-044: unknown routes 404 for everyone) |
