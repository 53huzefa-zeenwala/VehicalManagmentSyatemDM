import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../models/admin_models.dart';
import '../models/json.dart';
import '../models/models.dart';
import '../net/api_client.dart';
import '../time/gst.dart';
import 'repos.dart';

/// Admin calls (API_DOCUMENTATION.md §4.3–4.13), one method per endpoint. Every edit sends the record's
/// `rowVersion` back; a stale one answers 409 CONCURRENCY_CONFLICT.
class AdminRepos {
  const AdminRepos(this._api);

  final ApiClient _api;

  static String? _blank(String? value) => (value?.trim().isEmpty ?? true) ? null : value!.trim();

  static String? _date(DateTime? gstDate) => gstDate == null ? null : Gst.dateParam(gstDate);

  static String? _enum(Enum? value) => value == null ? null : apiName(value);

  // Dashboard and live ------------------------------------------------------------------------------

  Future<Dashboard> dashboard() => _api.get('dashboard', parse: (json) => Dashboard.fromJson(asJson(json)));

  /// Running trips, overdue first (TRP-05).
  Future<List<TripSummary>> liveTrips({int? lotId, int? typeId}) => _api.get(
    'trips/live',
    query: {'lotId': lotId, 'typeId': typeId},
    parse: (json) => asList(json, TripSummary.fromJson),
  );

  /// R-4 and admin trip lists; [fromGst]/[toGst] are GST dates of the trip start.
  Future<PagedResult<TripSummary>> tripLog({
    DateTime? fromGst,
    DateTime? toGst,
    TripStatus? status,
    int? staffId,
    int? vehicleId,
    int? lotId,
    int page = 1,
  }) => _api.get(
    'reports/trip-log',
    query: {
      'from': _date(fromGst),
      'to': _date(toGst),
      'status': _enum(status),
      'staffId': staffId,
      'vehicleId': vehicleId,
      'lotId': lotId,
      'page': page,
      'pageSize': 50,
    },
    parse: (json) => PagedResult.fromJson(asJson(json), TripSummary.fromJson),
  );

  // Transfers -----------------------------------------------------------------------------------------

  Future<Transfer> createTransfer({required int vehicleId, required int toLotId, String? note}) => _api.post(
    'transfers',
    body: {'vehicleId': vehicleId, 'toLotId': toLotId, 'note': _blank(note)},
    parse: (json) => Transfer.fromJson(asJson(json)),
  );

  Future<PagedResult<Transfer>> transfers({
    TransferStatus? status,
    int? lotId,
    int? vehicleId,
    DateTime? fromGst,
    DateTime? toGst,
    int page = 1,
  }) => _api.get(
    'transfers',
    query: {
      'status': _enum(status),
      'lotId': lotId,
      'vehicleId': vehicleId,
      'from': _date(fromGst),
      'to': _date(toGst),
      'page': page,
      'pageSize': 50,
    },
    parse: (json) => PagedResult.fromJson(asJson(json), Transfer.fromJson),
  );

  /// Only before departure (TRF-06).
  Future<Transfer> cancelTransfer(int id) =>
      _api.post('transfers/$id/cancel', parse: (json) => Transfer.fromJson(asJson(json)));

  // Vehicles ------------------------------------------------------------------------------------------

  Future<PagedResult<VehicleDetail>> vehicles({
    String? search,
    VehicleStatus? status,
    int? categoryId,
    int? typeId,
    int? lotId,
    bool notAtLot = false,
    int page = 1,
    int pageSize = 50,
  }) => _api.get(
    'vehicles',
    query: {
      'search': _blank(search),
      'status': _enum(status),
      'categoryId': categoryId,
      'typeId': typeId,
      'lotId': lotId,
      'notAtLot': notAtLot ? true : null,
      'page': page,
      'pageSize': pageSize,
    },
    parse: (json) => PagedResult.fromJson(asJson(json), VehicleDetail.fromJson),
  );

  Future<VehicleDetail> vehicle(int id) =>
      _api.get('vehicles/$id', parse: (json) => VehicleDetail.fromJson(asJson(json)));

  /// [lotId] null = "Not at a lot".
  Future<VehicleDetail> createVehicle({
    required String registrationNumber,
    required int vehicleTypeId,
    required String brand,
    required String model,
    required String colour,
    required int? lotId,
    required VehicleStatus status,
  }) => _api.post(
    'vehicles',
    body: {
      'registrationNumber': registrationNumber.trim(),
      'vehicleTypeId': vehicleTypeId,
      'brand': brand.trim(),
      'model': model.trim(),
      'colour': colour.trim(),
      'lotId': lotId,
      'status': apiName(status),
    },
    parse: (json) => VehicleDetail.fromJson(asJson(json)),
  );

  Future<VehicleDetail> updateVehicle(
    int id, {
    required String registrationNumber,
    required int vehicleTypeId,
    required String brand,
    required String model,
    required String colour,
    required String rowVersion,
  }) => _api.put(
    'vehicles/$id',
    body: {
      'registrationNumber': registrationNumber.trim(),
      'vehicleTypeId': vehicleTypeId,
      'brand': brand.trim(),
      'model': model.trim(),
      'colour': colour.trim(),
      'rowVersion': rowVersion,
    },
    parse: (json) => VehicleDetail.fromJson(asJson(json)),
  );

  /// Available, Inactive or UnderMaintenance; refused while the vehicle is out (D-043).
  Future<VehicleDetail> setVehicleStatus(int id, VehicleStatus status, String rowVersion) => _api.put(
    'vehicles/$id/status',
    body: {'status': apiName(status), 'rowVersion': rowVersion},
    parse: (json) => VehicleDetail.fromJson(asJson(json)),
  );

  /// Places the vehicle at an active lot, e.g. to resolve "Not at a lot" (VEH-05).
  Future<VehicleDetail> setVehicleLocation(int id, int lotId, String rowVersion) => _api.put(
    'vehicles/$id/location',
    body: {'lotId': lotId, 'rowVersion': rowVersion},
    parse: (json) => VehicleDetail.fromJson(asJson(json)),
  );

  Future<VehicleDetail> uploadVehiclePhoto(int id, List<int> jpegBytes) => _api.upload(
    'vehicles/$id/photo',
    bytes: jpegBytes,
    fileName: 'vehicle-$id.jpg',
    mimeType: 'image/jpeg',
    parse: (json) => VehicleDetail.fromJson(asJson(json)),
  );

  Future<PagedResult<VehicleHistoryEntry>> vehicleHistory(int id, {int page = 1}) => _api.get(
    'vehicles/$id/history',
    query: {'page': page, 'pageSize': 50},
    parse: (json) => PagedResult.fromJson(asJson(json), VehicleHistoryEntry.fromJson),
  );

  /// Label data for 1–500 vehicles (D-040).
  Future<List<QrLabel>> qrLabels(List<int> vehicleIds) => _api.get(
    'vehicles/qr-labels',
    query: {'ids': vehicleIds},
    parse: (json) => asList(json, QrLabel.fromJson),
  );

  // Categories and types --------------------------------------------------------------------------------

  Future<List<VehicleCategory>> categories({bool includeInactive = true}) => _api.get(
    'vehicle-categories',
    query: {'includeInactive': includeInactive ? true : null},
    parse: (json) => asList(json, VehicleCategory.fromJson),
  );

  Future<void> createCategory(String name) =>
      _api.post('vehicle-categories', body: {'name': name.trim()}, parse: (_) {});

  Future<void> updateCategory(int id, {required String name, required bool isActive}) =>
      _api.put('vehicle-categories/$id', body: {'name': name.trim(), 'isActive': isActive}, parse: (_) {});

  Future<void> createType(int categoryId, String name) =>
      _api.post('vehicle-categories/$categoryId/types', body: {'name': name.trim()}, parse: (_) {});

  Future<void> updateType(int id, {required String name, required bool isActive}) =>
      _api.put('vehicle-types/$id', body: {'name': name.trim(), 'isActive': isActive}, parse: (_) {});

  // Lots ----------------------------------------------------------------------------------------------

  Future<List<Lot>> lots({bool includeInactive = true}) => _api.get(
    'lots',
    query: {'includeInactive': includeInactive ? true : null},
    parse: (json) => asList(json, Lot.fromJson),
  );

  Future<Lot> lot(int id) => _api.get('lots/$id', parse: (json) => Lot.fromJson(asJson(json)));

  /// Create (no [id]) or edit. `typeCapacities` replaces the whole list.
  Future<Lot> saveLot(LotInput input, {int? id, String? rowVersion}) => id == null
      ? _api.post('lots', body: input.toJson(), parse: (json) => Lot.fromJson(asJson(json)))
      : _api.put(
          'lots/$id',
          body: {...input.toJson(), 'rowVersion': rowVersion},
          parse: (json) => Lot.fromJson(asJson(json)),
        );

  Future<Lot> setLotActive(int id, {required bool isActive, required String rowVersion}) => _api.put(
    'lots/$id/status',
    body: {'isActive': isActive, 'rowVersion': rowVersion},
    parse: (json) => Lot.fromJson(asJson(json)),
  );

  // Destinations --------------------------------------------------------------------------------------

  Future<List<Area>> areas({bool includeInactive = true}) => _api.get(
    'areas',
    query: {'includeInactive': includeInactive ? true : null},
    parse: (json) => asList(json, Area.fromJson),
  );

  Future<void> createArea(String name) => _api.post('areas', body: {'name': name.trim()}, parse: (_) {});

  Future<void> updateArea(int id, {required String name, required bool isActive}) =>
      _api.put('areas/$id', body: {'name': name.trim(), 'isActive': isActive}, parse: (_) {});

  Future<void> createLocation(int areaId, String name) =>
      _api.post('areas/$areaId/locations', body: {'name': name.trim()}, parse: (_) {});

  Future<void> updateLocation(int id, {required String name, required bool isActive}) =>
      _api.put('locations/$id', body: {'name': name.trim(), 'isActive': isActive}, parse: (_) {});

  // Staff ---------------------------------------------------------------------------------------------

  Future<PagedResult<StaffMember>> staffList({String? search, bool? isActive, int page = 1}) => _api.get(
    'staff',
    query: {'search': _blank(search), 'isActive': isActive, 'page': page, 'pageSize': 50},
    parse: (json) => PagedResult.fromJson(asJson(json), StaffMember.fromJson),
  );

  Future<StaffMember> staffMember(int id) =>
      _api.get('staff/$id', parse: (json) => StaffMember.fromJson(asJson(json)));

  /// The server generates the Staff ID (prefix + YY + 5 digits).
  Future<StaffMember> createStaff({
    required String fullName,
    required String mobile,
    required String? email,
    required String licenceNumber,
    required String password,
  }) => _api.post(
    'staff',
    body: {
      'fullName': fullName.trim(),
      'mobile': mobile.trim(),
      'email': _blank(email),
      'licenceNumber': licenceNumber.trim(),
      'password': password,
    },
    parse: (json) => StaffMember.fromJson(asJson(json)),
  );

  Future<StaffMember> updateStaff(
    int id, {
    required String fullName,
    required String mobile,
    required String? email,
    required String licenceNumber,
    required String rowVersion,
  }) => _api.put(
    'staff/$id',
    body: {
      'fullName': fullName.trim(),
      'mobile': mobile.trim(),
      'email': _blank(email),
      'licenceNumber': licenceNumber.trim(),
      'rowVersion': rowVersion,
    },
    parse: (json) => StaffMember.fromJson(asJson(json)),
  );

  /// Deactivating ends the person's sessions at once (AUTH-07).
  Future<StaffMember> setStaffActive(int id, {required bool isActive, required String rowVersion}) => _api.put(
    'staff/$id/status',
    body: {'isActive': isActive, 'rowVersion': rowVersion},
    parse: (json) => StaffMember.fromJson(asJson(json)),
  );

  /// Also unlocks the account and ends its sessions.
  Future<void> resetStaffPassword(int id, String newPassword) =>
      _api.postNoContent('staff/$id/reset-password', body: {'newPassword': newPassword});

  Future<StaffMember> uploadStaffPhoto(int id, List<int> jpegBytes) => _api.upload(
    'staff/$id/photo',
    bytes: jpegBytes,
    fileName: 'staff-$id.jpg',
    mimeType: 'image/jpeg',
    parse: (json) => StaffMember.fromJson(asJson(json)),
  );

  Future<StaffTrips> staffTrips(int id, {required DateTime fromGst, required DateTime toGst, int page = 1}) => _api.get(
    'staff/$id/trips',
    query: {'from': _date(fromGst), 'to': _date(toGst), 'page': page, 'pageSize': 50},
    parse: (json) => StaffTrips.fromJson(asJson(json)),
  );

  // Admins --------------------------------------------------------------------------------------------

  Future<List<AdminAccount>> admins() => _api.get('admins', parse: (json) => asList(json, AdminAccount.fromJson));

  Future<AdminAccount> admin(int id) =>
      _api.get('admins/$id', parse: (json) => AdminAccount.fromJson(asJson(json)));

  Future<AdminAccount> createAdmin({
    required String fullName,
    required String mobile,
    required String? email,
    required String password,
  }) => _api.post(
    'admins',
    body: {'fullName': fullName.trim(), 'mobile': mobile.trim(), 'email': _blank(email), 'password': password},
    parse: (json) => AdminAccount.fromJson(asJson(json)),
  );

  Future<AdminAccount> updateAdmin(
    int id, {
    required String fullName,
    required String mobile,
    required String? email,
    required String rowVersion,
  }) => _api.put(
    'admins/$id',
    body: {'fullName': fullName.trim(), 'mobile': mobile.trim(), 'email': _blank(email), 'rowVersion': rowVersion},
    parse: (json) => AdminAccount.fromJson(asJson(json)),
  );

  /// Not allowed on yourself (CANNOT_DEACTIVATE_SELF).
  Future<AdminAccount> setAdminActive(int id, {required bool isActive, required String rowVersion}) => _api.put(
    'admins/$id/status',
    body: {'isActive': isActive, 'rowVersion': rowVersion},
    parse: (json) => AdminAccount.fromJson(asJson(json)),
  );

  Future<void> resetAdminPassword(int id, String newPassword) =>
      _api.postNoContent('admins/$id/reset-password', body: {'newPassword': newPassword});

  // Settings and audit log --------------------------------------------------------------------------------

  Future<AppSettings> settings() => _api.get('settings', parse: (json) => AppSettings.fromJson(asJson(json)));

  Future<AppSettings> updateSettings({
    required String staffIdPrefix,
    required int reminderDelayMinutes,
    required String rowVersion,
  }) => _api.put(
    'settings',
    body: {
      'staffIdPrefix': staffIdPrefix.trim().toUpperCase(),
      'reminderDelayMinutes': reminderDelayMinutes,
      'rowVersion': rowVersion,
    },
    parse: (json) => AppSettings.fromJson(asJson(json)),
  );

  Future<PagedResult<AuditEntry>> auditLog({
    int? userId,
    String? entityType,
    int? entityId,
    DateTime? fromGst,
    DateTime? toGst,
    int page = 1,
  }) => _api.get(
    'audit-logs',
    query: {
      'userId': userId,
      'entityType': _blank(entityType),
      'entityId': entityId,
      'from': _date(fromGst),
      'to': _date(toGst),
      'page': page,
      'pageSize': 50,
    },
    parse: (json) => PagedResult.fromJson(asJson(json), AuditEntry.fromJson),
  );

  // Reports (on screen only, no exports: D-012) --------------------------------------------------------

  /// R-1; the range is required and at most 366 days.
  Future<VehicleHoursReport> vehicleHours({
    required DateTime fromGst,
    required DateTime toGst,
    int? lotId,
    int? categoryId,
    int? typeId,
    int? vehicleId,
  }) => _api.get(
    'reports/vehicle-hours',
    query: {
      'from': _date(fromGst),
      'to': _date(toGst),
      'lotId': lotId,
      'categoryId': categoryId,
      'typeId': typeId,
      'vehicleId': vehicleId,
    },
    parse: (json) => VehicleHoursReport.fromJson(asJson(json)),
  );

  /// R-2; the range is required and at most 366 days.
  Future<StaffHoursReport> staffHours({required DateTime fromGst, required DateTime toGst, int? staffId}) => _api.get(
    'reports/staff-hours',
    query: {'from': _date(fromGst), 'to': _date(toGst), 'staffId': staffId},
    parse: (json) => StaffHoursReport.fromJson(asJson(json)),
  );

  /// R-3; without dates the period is today.
  Future<LotUtilizationReport> lotUtilization({DateTime? fromGst, DateTime? toGst, int? lotId}) => _api.get(
    'reports/lot-utilization',
    query: {'from': _date(fromGst), 'to': _date(toGst), 'lotId': lotId},
    parse: (json) => LotUtilizationReport.fromJson(asJson(json)),
  );

  /// R-5.
  Future<PagedResult<Transfer>> transferLog({
    TransferStatus? status,
    int? lotId,
    int? vehicleId,
    DateTime? fromGst,
    DateTime? toGst,
    int page = 1,
  }) => _api.get(
    'reports/transfers',
    query: {
      'status': _enum(status),
      'lotId': lotId,
      'vehicleId': vehicleId,
      'from': _date(fromGst),
      'to': _date(toGst),
      'page': page,
      'pageSize': 50,
    },
    parse: (json) => PagedResult.fromJson(asJson(json), Transfer.fromJson),
  );
}

/// What the lot form sends (POST/PUT lots). No location: no map in v1 (D-056).
class LotInput {
  const LotInput({
    required this.code,
    required this.name,
    required this.address,
    required this.contactName,
    required this.contactNumber,
    required this.lotType,
    required this.totalCapacity,
    required this.baselineOccupied,
    required this.typeCapacities,
    this.opensAt,
    this.closesAt,
  });

  final String code;
  final String name;
  final String address;
  final String contactName;
  final String contactNumber;

  /// `HH:mm` (GST); both null = open 24 hours.
  final String? opensAt;
  final String? closesAt;
  final LotType lotType;
  final int totalCapacity;
  final int baselineOccupied;
  final List<TypeCapacityInput> typeCapacities;

  Json toJson() => {
    'code': code.trim().toUpperCase(),
    'name': name.trim(),
    'address': address.trim(),
    'contactName': contactName.trim(),
    'contactNumber': contactNumber.trim(),
    'opensAt': opensAt,
    'closesAt': closesAt,
    'lotType': apiName(lotType),
    'totalCapacity': totalCapacity,
    'baselineOccupied': baselineOccupied,
    'typeCapacities': [
      for (final t in typeCapacities)
        {'vehicleTypeId': t.vehicleTypeId, 'capacity': t.capacity, 'baselineOccupied': t.baselineOccupied},
    ],
  };
}

class TypeCapacityInput {
  const TypeCapacityInput({required this.vehicleTypeId, required this.capacity, required this.baselineOccupied});

  final int vehicleTypeId;
  final int capacity;
  final int baselineOccupied;
}

final adminReposProvider = Provider<AdminRepos>((ref) => AdminRepos(ref.watch(apiClientProvider)));
