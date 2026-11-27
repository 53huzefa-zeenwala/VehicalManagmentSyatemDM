import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../models/json.dart';
import '../models/models.dart';
import '../net/api_client.dart';
import '../time/gst.dart';

/// The API client; each app overrides this in `main()` after creating it.
final apiClientProvider = Provider<ApiClient>((ref) => throw UnimplementedError('apiClientProvider must be overridden'));

/// Typed calls to the API, one group per area. Each method maps to one endpoint in API_DOCUMENTATION.md.
class Repos {
  const Repos(this._api);

  final ApiClient _api;

  // Auth
  Future<Me> login(String loginId, String password) => _api.post(
    'auth/login',
    body: {'loginId': loginId.trim(), 'password': password, 'rememberMe': true},
    parse: (json) => Me.fromJson(asJson(json)),
  );

  Future<Me> me() => _api.get('auth/me', parse: (json) => Me.fromJson(asJson(json)));

  Future<void> logout() => _api.postNoContent('auth/logout');

  Future<void> changePassword(String currentPassword, String newPassword) => _api.postNoContent(
    'auth/change-password',
    body: {'currentPassword': currentPassword, 'newPassword': newPassword},
  );

  // Vehicles
  Future<VehicleLookup> lookupVehicle({String? qr, String? registration}) => _api.get(
    'vehicles/lookup',
    query: {'qr': qr, 'registration': registration},
    parse: (json) => VehicleLookup.fromJson(asJson(json)),
  );

  // Trips
  Future<TripDetail?> activeTrip() =>
      _api.get('trips/active', parse: (json) => json == null ? null : TripDetail.fromJson(asJson(json)));

  Future<TripDetail> trip(int id) => _api.get('trips/$id', parse: (json) => TripDetail.fromJson(asJson(json)));

  Future<PagedResult<TripSummary>> trips({required DateTime fromGst, required DateTime toGst, int page = 1}) => _api.get(
    'trips',
    query: {'from': Gst.dateParam(fromGst), 'to': Gst.dateParam(toGst), 'page': page, 'pageSize': 50},
    parse: (json) => PagedResult.fromJson(asJson(json), TripSummary.fromJson),
  );

  Future<TripDetail> startTrip({
    String? vehicleQrToken,
    String? vehicleRegistration,
    required int destinationLocationId,
    required String signaturePngBase64,
  }) => _api.post(
    'trips',
    body: {
      'vehicleQrToken': ?vehicleQrToken,
      'vehicleRegistration': ?vehicleRegistration,
      'destinationLocationId': destinationLocationId,
      'signaturePngBase64': signaturePngBase64,
    },
    parse: (json) => TripDetail.fromJson(asJson(json)),
  );

  Future<TripDetail> swapVehicle(
    int tripId, {
    String? newVehicleQrToken,
    String? newVehicleRegistration,
    required String reason,
    required int? oldVehicleLotId,
  }) => _api.post(
    'trips/$tripId/swap-vehicle',
    body: {
      'newVehicleQrToken': ?newVehicleQrToken,
      'newVehicleRegistration': ?newVehicleRegistration,
      'reason': reason,
      'oldVehicleLotId': oldVehicleLotId,
    },
    parse: (json) => TripDetail.fromJson(asJson(json)),
  );

  Future<List<StaffRef>> handoverCandidates({String? search}) => _api.get(
    'staff/handover-candidates',
    query: {'search': (search?.trim().isEmpty ?? true) ? null : search!.trim()},
    parse: (json) => asList(json, StaffRef.fromJson),
  );

  Future<TripDetail> handOver(int tripId, {required int toStaffId, required String reason}) => _api.post(
    'trips/$tripId/handover',
    body: {'toStaffId': toStaffId, 'reason': reason},
    parse: (json) => TripDetail.fromJson(asJson(json)),
  );

  Future<TripDetail> cancelTrip(
    int tripId, {
    required CancelCategory category,
    required String note,
    required int? vehicleLotId,
  }) => _api.post(
    'trips/$tripId/cancel',
    body: {'category': apiName(category), 'note': note, 'vehicleLotId': vehicleLotId},
    parse: (json) => TripDetail.fromJson(asJson(json)),
  );

  Future<TripDetail> endTrip(
    int tripId, {
    String? vehicleQrToken,
    String? vehicleRegistration,
    required int returnLotId,
  }) => _api.post(
    'trips/$tripId/end',
    body: {
      'vehicleQrToken': ?vehicleQrToken,
      'vehicleRegistration': ?vehicleRegistration,
      'returnLotId': returnLotId,
    },
    parse: (json) => TripDetail.fromJson(asJson(json)),
  );

  // Lots and destinations
  Future<List<Lot>> lots() => _api.get('lots', parse: (json) => asList(json, Lot.fromJson));

  Future<List<Area>> areas() => _api.get('areas', parse: (json) => asList(json, Area.fromJson));

  // Transfers
  Future<TransferTasks> transferTasks() =>
      _api.get('transfers/tasks', parse: (json) => TransferTasks.fromJson(asJson(json)));

  Future<Transfer> transfer(int id) => _api.get('transfers/$id', parse: (json) => Transfer.fromJson(asJson(json)));

  Future<Transfer> transferByVehicle(String qr) =>
      _api.get('transfers/by-vehicle', query: {'qr': qr}, parse: (json) => Transfer.fromJson(asJson(json)));

  Future<Transfer> departTransfer(int id) =>
      _api.post('transfers/$id/depart', parse: (json) => Transfer.fromJson(asJson(json)));

  Future<Transfer> arriveTransfer(int id) =>
      _api.post('transfers/$id/arrive', parse: (json) => Transfer.fromJson(asJson(json)));

  // Notifications
  Future<NotificationPage> notifications({int page = 1, bool unreadOnly = false}) => _api.get(
    'notifications',
    query: {'page': page, 'pageSize': 50, 'unreadOnly': unreadOnly ? true : null},
    parse: (json) => NotificationPage.fromJson(asJson(json)),
  );

  Future<void> markNotificationRead(int id) => _api.postNoContent('notifications/$id/read');

  Future<void> markAllNotificationsRead() => _api.postNoContent('notifications/read-all');
}

final reposProvider = Provider<Repos>((ref) => Repos(ref.watch(apiClientProvider)));
