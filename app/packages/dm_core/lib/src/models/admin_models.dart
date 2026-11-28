import 'json.dart';
import 'models.dart';

// Admin-only shapes (API_DOCUMENTATION.md §4.3–4.13). `rowVersion` is the base64 concurrency token the API
// expects back on every edit; a stale one answers 409 CONCURRENCY_CONFLICT.

/// `GET vehicles/{id}` and the list rows of `GET vehicles`.
class VehicleDetail {
  const VehicleDetail({
    required this.id,
    required this.registrationNumber,
    required this.categoryId,
    required this.category,
    required this.vehicleTypeId,
    required this.type,
    required this.brand,
    required this.model,
    required this.colour,
    required this.status,
    required this.isNotAtLot,
    required this.qrToken,
    required this.rowVersion,
    this.photoUrl,
    this.currentLot,
    this.createdAtUtc,
    this.updatedAtUtc,
  });

  factory VehicleDetail.fromJson(Json json) => VehicleDetail(
    id: integer(json, 'id'),
    registrationNumber: str(json, 'registrationNumber'),
    categoryId: integer(json, 'categoryId'),
    category: str(json, 'category'),
    vehicleTypeId: integer(json, 'vehicleTypeId'),
    type: str(json, 'type'),
    brand: str(json, 'brand'),
    model: str(json, 'model'),
    colour: str(json, 'colour'),
    photoUrl: strOrNull(json, 'photoUrl'),
    status: enumOf(VehicleStatus.values, json['status'], VehicleStatus.inactive),
    currentLot: LotRef.maybe(json['currentLot']),
    isNotAtLot: boolean(json, 'isNotAtLot'),
    qrToken: str(json, 'qrToken'),
    createdAtUtc: utcOrNull(json, 'createdAtUtc'),
    updatedAtUtc: utcOrNull(json, 'updatedAtUtc'),
    rowVersion: str(json, 'rowVersion'),
  );

  final int id;
  final String registrationNumber;
  final int categoryId;
  final String category;
  final int vehicleTypeId;
  final String type;
  final String brand;
  final String model;
  final String colour;
  final String? photoUrl;
  final VehicleStatus status;

  /// Null when the vehicle is out (trip or transfer) or "Not at a lot".
  final LotRef? currentLot;

  /// Left somewhere other than a lot; an admin places it at a lot (VEH-05).
  final bool isNotAtLot;
  final String qrToken;
  final DateTime? createdAtUtc;
  final DateTime? updatedAtUtc;
  final String rowVersion;

  VehicleRef get ref => VehicleRef(id: id, registrationNumber: registrationNumber, category: category, type: type);
}

/// One line of `GET vehicles/{id}/history`. Kind: Trip, Transfer, or an admin action
/// (Create, Update, StatusChange, LocationResolve).
class VehicleHistoryEntry {
  const VehicleHistoryEntry({
    required this.atUtc,
    required this.kind,
    required this.description,
    this.endedAtUtc,
    this.durationSeconds,
    this.tripId,
    this.transferId,
  });

  factory VehicleHistoryEntry.fromJson(Json json) => VehicleHistoryEntry(
    atUtc: utc(json, 'atUtc'),
    endedAtUtc: utcOrNull(json, 'endedAtUtc'),
    kind: str(json, 'kind'),
    description: str(json, 'description'),
    durationSeconds: intOrNull(json, 'durationSeconds'),
    tripId: intOrNull(json, 'tripId'),
    transferId: intOrNull(json, 'transferId'),
  );

  final DateTime atUtc;
  final DateTime? endedAtUtc;
  final String kind;
  final String description;
  final int? durationSeconds;
  final int? tripId;
  final int? transferId;
}

/// `GET vehicles/qr-labels`: what the app needs to draw a QR label (D-040).
class QrLabel {
  const QrLabel({
    required this.vehicleId,
    required this.qrToken,
    required this.registrationNumber,
    required this.category,
    required this.type,
  });

  factory QrLabel.fromJson(Json json) => QrLabel(
    vehicleId: integer(json, 'vehicleId'),
    qrToken: str(json, 'qrToken'),
    registrationNumber: str(json, 'registrationNumber'),
    category: str(json, 'category'),
    type: str(json, 'type'),
  );

  final int vehicleId;
  final String qrToken;
  final String registrationNumber;
  final String category;
  final String type;
}

class VehicleTypeItem {
  const VehicleTypeItem({required this.id, required this.name, required this.isActive});

  factory VehicleTypeItem.fromJson(Json json) =>
      VehicleTypeItem(id: integer(json, 'id'), name: str(json, 'name'), isActive: json['isActive'] as bool? ?? true);

  final int id;
  final String name;
  final bool isActive;
}

/// `GET vehicle-categories`: a category with its types.
class VehicleCategory {
  const VehicleCategory({required this.id, required this.name, required this.isActive, required this.types});

  factory VehicleCategory.fromJson(Json json) => VehicleCategory(
    id: integer(json, 'id'),
    name: str(json, 'name'),
    isActive: json['isActive'] as bool? ?? true,
    types: asList(json['types'], VehicleTypeItem.fromJson),
  );

  final int id;
  final String name;
  final bool isActive;
  final List<VehicleTypeItem> types;
}

/// A staff account (driver), `GET staff/{id}`.
class StaffMember {
  const StaffMember({
    required this.id,
    required this.loginId,
    required this.fullName,
    required this.mobile,
    required this.licenceNumber,
    required this.isActive,
    required this.rowVersion,
    this.email,
    this.photoUrl,
    this.createdAtUtc,
  });

  factory StaffMember.fromJson(Json json) => StaffMember(
    id: integer(json, 'id'),
    loginId: str(json, 'loginId'),
    fullName: str(json, 'fullName'),
    mobile: str(json, 'mobile'),
    email: strOrNull(json, 'email'),
    licenceNumber: str(json, 'licenceNumber'),
    photoUrl: strOrNull(json, 'photoUrl'),
    isActive: boolean(json, 'isActive'),
    createdAtUtc: utcOrNull(json, 'createdAtUtc'),
    rowVersion: str(json, 'rowVersion'),
  );

  final int id;
  final String loginId;
  final String fullName;
  final String mobile;
  final String? email;
  final String licenceNumber;
  final String? photoUrl;
  final bool isActive;
  final DateTime? createdAtUtc;
  final String rowVersion;
}

/// An admin account, `GET admins/{id}`.
class AdminAccount {
  const AdminAccount({
    required this.id,
    required this.loginId,
    required this.fullName,
    required this.mobile,
    required this.isActive,
    required this.rowVersion,
    this.email,
    this.createdAtUtc,
  });

  factory AdminAccount.fromJson(Json json) => AdminAccount(
    id: integer(json, 'id'),
    loginId: str(json, 'loginId'),
    fullName: str(json, 'fullName'),
    mobile: str(json, 'mobile'),
    email: strOrNull(json, 'email'),
    isActive: boolean(json, 'isActive'),
    createdAtUtc: utcOrNull(json, 'createdAtUtc'),
    rowVersion: str(json, 'rowVersion'),
  );

  final int id;
  final String loginId;
  final String fullName;
  final String mobile;
  final String? email;
  final bool isActive;
  final DateTime? createdAtUtc;
  final String rowVersion;
}

/// `GET staff/{id}/trips`: the trips a staff member took part in and their hours in the range (STF-03).
class StaffTrips {
  const StaffTrips({required this.staff, required this.totalSeconds, required this.trips});

  factory StaffTrips.fromJson(Json json) => StaffTrips(
    staff: StaffRef.fromJson(asJson(json['staff'])),
    totalSeconds: integer(json, 'totalSeconds'),
    trips: PagedResult.fromJson(asJson(json['trips']), TripSummary.fromJson),
  );

  final StaffRef staff;
  final int totalSeconds;
  final PagedResult<TripSummary> trips;
}

/// `GET settings` (SET-01).
class AppSettings {
  const AppSettings({
    required this.staffIdPrefix,
    required this.reminderDelayMinutes,
    required this.reminderRepeatMinutes,
    required this.rowVersion,
  });

  factory AppSettings.fromJson(Json json) => AppSettings(
    staffIdPrefix: str(json, 'staffIdPrefix'),
    reminderDelayMinutes: integer(json, 'reminderDelayMinutes'),
    reminderRepeatMinutes: integer(json, 'reminderRepeatMinutes'),
    rowVersion: str(json, 'rowVersion'),
  );

  final String staffIdPrefix;
  final int reminderDelayMinutes;
  final int reminderRepeatMinutes;
  final String rowVersion;
}

/// One row of `GET audit-logs` (AUD-02). Before/after hold only the changed fields (passwords never appear).
class AuditEntry {
  const AuditEntry({
    required this.id,
    required this.atUtc,
    required this.user,
    required this.action,
    required this.entityType,
    required this.entityId,
    this.before,
    this.after,
  });

  factory AuditEntry.fromJson(Json json) => AuditEntry(
    id: integer(json, 'id'),
    atUtc: utc(json, 'atUtc'),
    user: StaffRef.fromJson(asJson(json['user'])),
    action: str(json, 'action'),
    entityType: str(json, 'entityType'),
    entityId: integer(json, 'entityId'),
    before: json['before'] is Map<String, Object?> ? json['before']! as Json : null,
    after: json['after'] is Map<String, Object?> ? json['after']! as Json : null,
  );

  final int id;
  final DateTime atUtc;
  final StaffRef user;
  final String action;
  final String entityType;
  final int entityId;
  final Json? before;
  final Json? after;
}

class HourCount {
  const HourCount({required this.hour, required this.trips});

  factory HourCount.fromJson(Json json) => HourCount(hour: integer(json, 'hour'), trips: integer(json, 'trips'));

  final int hour;
  final int trips;
}

class LotAttention {
  const LotAttention({required this.lot, required this.capacity, required this.occupied});

  factory LotAttention.fromJson(Json json) => LotAttention(
    lot: LotRef.fromJson(asJson(json['lot'])),
    capacity: integer(json, 'capacity'),
    occupied: integer(json, 'occupied'),
  );

  final LotRef lot;
  final int capacity;
  final int occupied;
}

class NeedsAttention {
  const NeedsAttention({
    required this.overdueTrips,
    required this.overdueTripList,
    required this.overdueTransfers,
    required this.vehiclesNotAtLot,
    required this.vehiclesNotAtLotList,
    required this.lotsOverCapacity,
    required this.transfersWaiting,
    required this.transfersWaitingList,
  });

  factory NeedsAttention.fromJson(Json json) => NeedsAttention(
    overdueTrips: integer(json, 'overdueTrips'),
    overdueTripList: asList(json['overdueTripList'], TripSummary.fromJson),
    overdueTransfers: integer(json, 'overdueTransfers'),
    vehiclesNotAtLot: integer(json, 'vehiclesNotAtLot'),
    vehiclesNotAtLotList: asList(json['vehiclesNotAtLotList'], VehicleRef.fromJson),
    lotsOverCapacity: asList(json['lotsOverCapacity'], LotAttention.fromJson),
    transfersWaiting: integer(json, 'transfersWaiting'),
    transfersWaitingList: asList(json['transfersWaitingList'], Transfer.fromJson),
  );

  final int overdueTrips;
  final List<TripSummary> overdueTripList;
  final int overdueTransfers;
  final int vehiclesNotAtLot;
  final List<VehicleRef> vehiclesNotAtLotList;
  final List<LotAttention> lotsOverCapacity;
  final int transfersWaiting;
  final List<Transfer> transfersWaitingList;

  bool get isEmpty =>
      overdueTrips == 0 && overdueTransfers == 0 && vehiclesNotAtLot == 0 && lotsOverCapacity.isEmpty && transfersWaiting == 0;
}

/// `GET dashboard` (A-01, RPT-01): today's figures (GST day) and what needs attention.
class Dashboard {
  const Dashboard({
    required this.onTripNow,
    required this.transfersInTransit,
    required this.tripsToday,
    required this.secondsOutToday,
    required this.freeSpaces,
    required this.tripsPerHourToday,
    required this.needsAttention,
  });

  factory Dashboard.fromJson(Json json) => Dashboard(
    onTripNow: integer(json, 'onTripNow'),
    transfersInTransit: integer(json, 'transfersInTransit'),
    tripsToday: integer(json, 'tripsToday'),
    secondsOutToday: integer(json, 'secondsOutToday'),
    freeSpaces: integer(json, 'freeSpaces'),
    tripsPerHourToday: asList(json['tripsPerHourToday'], HourCount.fromJson),
    needsAttention: NeedsAttention.fromJson(asJson(json['needsAttention'])),
  );

  final int onTripNow;
  final int transfersInTransit;
  final int tripsToday;
  final int secondsOutToday;
  final int freeSpaces;
  final List<HourCount> tripsPerHourToday;
  final NeedsAttention needsAttention;
}

/// Seconds on one GST day (`date` is `yyyy-MM-dd`).
class DaySeconds {
  const DaySeconds({required this.date, required this.seconds});

  factory DaySeconds.fromJson(Json json) => DaySeconds(date: str(json, 'date'), seconds: integer(json, 'seconds'));

  final String date;
  final int seconds;
}

class VehicleHoursRow {
  const VehicleHoursRow({
    required this.vehicle,
    required this.tripSeconds,
    required this.transferSeconds,
    required this.totalSeconds,
    required this.days,
  });

  factory VehicleHoursRow.fromJson(Json json) => VehicleHoursRow(
    vehicle: VehicleRef.fromJson(asJson(json['vehicle'])),
    tripSeconds: integer(json, 'tripSeconds'),
    transferSeconds: integer(json, 'transferSeconds'),
    totalSeconds: integer(json, 'totalSeconds'),
    days: asList(json['days'], DaySeconds.fromJson),
  );

  final VehicleRef vehicle;
  final int tripSeconds;
  final int transferSeconds;
  final int totalSeconds;
  final List<DaySeconds> days;
}

/// R-1 vehicle hours.
class VehicleHoursReport {
  const VehicleHoursReport({required this.totalSeconds, required this.vehicles, required this.rows});

  factory VehicleHoursReport.fromJson(Json json) => VehicleHoursReport(
    totalSeconds: integer(json, 'totalSeconds'),
    vehicles: integer(json, 'vehicles'),
    rows: asList(json['rows'], VehicleHoursRow.fromJson),
  );

  final int totalSeconds;
  final int vehicles;
  final List<VehicleHoursRow> rows;
}

class StaffHoursRow {
  const StaffHoursRow({
    required this.staff,
    required this.trips,
    required this.transfers,
    required this.tripSeconds,
    required this.transferSeconds,
    required this.totalSeconds,
    required this.days,
  });

  factory StaffHoursRow.fromJson(Json json) => StaffHoursRow(
    staff: StaffRef.fromJson(asJson(json['staff'])),
    trips: integer(json, 'trips'),
    transfers: integer(json, 'transfers'),
    tripSeconds: integer(json, 'tripSeconds'),
    transferSeconds: integer(json, 'transferSeconds'),
    totalSeconds: integer(json, 'totalSeconds'),
    days: asList(json['days'], DaySeconds.fromJson),
  );

  final StaffRef staff;
  final int trips;
  final int transfers;
  final int tripSeconds;
  final int transferSeconds;
  final int totalSeconds;
  final List<DaySeconds> days;
}

/// R-2 staff (driver) hours.
class StaffHoursReport {
  const StaffHoursReport({required this.totalSeconds, required this.staff, required this.rows});

  factory StaffHoursReport.fromJson(Json json) => StaffHoursReport(
    totalSeconds: integer(json, 'totalSeconds'),
    staff: integer(json, 'staff'),
    rows: asList(json['rows'], StaffHoursRow.fromJson),
  );

  final int totalSeconds;
  final int staff;
  final List<StaffHoursRow> rows;
}

class LotUtilizationRow {
  const LotUtilizationRow({
    required this.lot,
    required this.isActive,
    required this.total,
    required this.byType,
    required this.tripsStarted,
    required this.tripsReturned,
    required this.transfersOut,
    required this.transfersIn,
  });

  factory LotUtilizationRow.fromJson(Json json) => LotUtilizationRow(
    lot: LotRef.fromJson(asJson(json['lot'])),
    isActive: json['isActive'] as bool? ?? true,
    total: LotTotals.fromJson(asJson(json['total'])),
    byType: asList(json['byType'], LotTypeOccupancy.fromJson),
    tripsStarted: integer(json, 'tripsStarted'),
    tripsReturned: integer(json, 'tripsReturned'),
    transfersOut: integer(json, 'transfersOut'),
    transfersIn: integer(json, 'transfersIn'),
  );

  final LotRef lot;
  final bool isActive;
  final LotTotals total;
  final List<LotTypeOccupancy> byType;
  final int tripsStarted;
  final int tripsReturned;
  final int transfersOut;
  final int transfersIn;
}

/// R-3 lot utilisation: current occupancy plus movements in the period.
class LotUtilizationReport {
  const LotUtilizationReport({required this.from, required this.to, required this.rows});

  factory LotUtilizationReport.fromJson(Json json) => LotUtilizationReport(
    from: str(json, 'from'),
    to: str(json, 'to'),
    rows: asList(json['rows'], LotUtilizationRow.fromJson),
  );

  final String from;
  final String to;
  final List<LotUtilizationRow> rows;
}
