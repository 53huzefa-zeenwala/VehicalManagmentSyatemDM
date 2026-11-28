import 'package:dm_core/dm_core.dart';
import 'package:flutter_test/flutter_test.dart';

// JSON shaped exactly as API_DOCUMENTATION.md v1.0 / the API's DTOs return it.
const _lotRef = {'id': 1, 'code': 'TEST-A', 'name': 'Test Lot A'};
const _staff = {'id': 7, 'loginId': 'DM2600001', 'fullName': 'Test Driver One', 'photoUrl': null};
const _vehicleCard = {
  'id': 3,
  'registrationNumber': 'TEST-1001',
  'category': 'Test Car',
  'type': 'Test Sedan',
  'brand': 'Toyota',
  'model': 'Camry',
  'colour': 'White',
  'photoUrl': null,
  'status': 'Active',
  'currentLot': null,
};

void main() {
  test('trip detail parses refs, segments, enums and UTC times', () {
    final trip = TripDetail.fromJson({
      'id': 42,
      'status': 'Active',
      'startedAtUtc': '2026-11-27T08:15:00.000Z',
      'endedAtUtc': null,
      'totalSeconds': null,
      'isOverdue': true,
      'nextReminderAtUtc': '2026-11-27T10:15:00.000Z',
      'originLot': _lotRef,
      'destination': {'locationId': 5, 'area': 'Test Downtown', 'location': 'Test Mall Entrance'},
      'currentStaff': _staff,
      'currentVehicle': _vehicleCard,
      'signatureUrl': '/api/v1/files/signatures/abc.png',
      'segments': [
        {
          'seq': 1,
          'kind': 'Start',
          'staff': _staff,
          'vehicle': {'id': 3, 'registrationNumber': 'TEST-1001', 'category': 'Test Car', 'type': 'Test Sedan'},
          'startedAtUtc': '2026-11-27T08:15:00.000Z',
          'endedAtUtc': null,
          'reason': null,
          'outgoingVehicleLot': null,
          'outgoingVehicleNotAtLot': false,
        },
      ],
      'returnLot': null,
      'cancel': null,
      'warnings': [],
      'reminders': [],
    });

    expect(trip.id, 42);
    expect(trip.isActive, isTrue);
    expect(trip.isOverdue, isTrue);
    expect(trip.startedAtUtc, DateTime.utc(2026, 11, 27, 8, 15));
    expect(trip.startedAtUtc.isUtc, isTrue);
    expect(trip.originLot.code, 'TEST-A');
    expect(trip.destination.label, 'Test Mall Entrance · Test Downtown');
    expect(trip.currentVehicle.status, VehicleStatus.active);
    expect(trip.currentVehicle.currentLot, isNull);
    expect(trip.segments.single.kind, SegmentKind.start);
  });

  test('lot parses totals and per-type capacity (null when not set)', () {
    final lot = Lot.fromJson({
      'id': 1,
      'code': 'TEST-A',
      'name': 'Test Lot A',
      'address': 'Somewhere',
      'contactName': 'Test Contact',
      'contactNumber': '+971500000001',
      'opensAt': null,
      'closesAt': null,
      'lotType': 'Underground',
      'isActive': true,
      'baselineOccupied': 0,
      'total': {'capacity': 30, 'occupied': 27, 'available': 3, 'isFull': false, 'isOverCapacity': false},
      'byType': [
        {'vehicleTypeId': 1, 'type': 'Test Sedan', 'category': 'Test Car', 'capacity': 15, 'baselineOccupied': 0, 'occupied': 2, 'available': 13},
        {'vehicleTypeId': 2, 'type': 'Test Bus', 'category': 'Test Car', 'capacity': null, 'baselineOccupied': 0, 'occupied': 0, 'available': null},
      ],
      'rowVersion': 'AAAAAAAAB9E=',
    });

    expect(lot.lotType, LotType.underground);
    expect(lot.hours, 'Open 24 hours');
    expect(lot.total.fill, closeTo(.9, .001));
    expect(lot.byType.last.capacity, isNull);
  });

  test('unknown enum values from a newer server fall back instead of crashing', () {
    final item = AppNotification.fromJson({
      'id': 9,
      'type': 'SomethingNew',
      'title': 't',
      'body': 'b',
      'targetType': null,
      'targetId': null,
      'createdAtUtc': '2026-11-27T08:15:00Z',
      'readAtUtc': null,
    });

    expect(item.type, NotificationType.unknown);
    expect(item.isRead, isFalse);
  });

  test('enum names are sent in the API casing', () {
    expect(apiName(CancelCategory.technical), 'Technical');
    expect(apiName(TransferStatus.pendingDeparture), 'PendingDeparture');
  });

  test('me reads staff month stats', () {
    final me = Me.fromJson({
      'id': 7,
      'loginId': 'DM2600001',
      'role': 'Staff',
      'fullName': 'Test Driver One',
      'mobile': '+971500000011',
      'email': null,
      'licenceNumber': 'TEST-LIC-001',
      'photoUrl': null,
      'hasActiveTrip': false,
      'thisMonth': {'trips': 3, 'totalSeconds': 5400},
    });

    expect(me.role, UserRole.staff);
    expect(me.firstName, 'Test');
    expect(me.thisMonth!.totalSeconds, 5400);
  });
}
