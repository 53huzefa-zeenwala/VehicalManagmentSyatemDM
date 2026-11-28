import 'package:dm_core/dm_core.dart';
import 'package:flutter_test/flutter_test.dart';

// JSON shaped exactly as the API's admin DTOs return it (API_DOCUMENTATION.md §4.3–4.13).
const _lotRef = {'id': 1, 'code': 'TEST-A', 'name': 'Test Lot A'};
const _staffRef = {'id': 7, 'loginId': 'DM2600001', 'fullName': 'Test Driver One', 'photoUrl': null};
const _vehicleRef = {'id': 3, 'registrationNumber': 'TEST-1001', 'category': 'Test Car', 'type': 'Test Sedan'};

void main() {
  test('vehicle detail keeps the QR token, lot, "Not at a lot" flag and rowVersion', () {
    final v = VehicleDetail.fromJson({
      'id': 12,
      'registrationNumber': 'D 01423',
      'categoryId': 1,
      'category': 'Truck',
      'vehicleTypeId': 2,
      'type': 'Garbage truck',
      'brand': 'Isuzu',
      'model': 'NPR',
      'colour': 'White',
      'photoUrl': '/api/v1/files/vehicles/abc.jpg',
      'status': 'UnderMaintenance',
      'currentLot': _lotRef,
      'isNotAtLot': false,
      'qrToken': 'q8Jd0abc',
      'createdAtUtc': '2026-11-27T08:00:00Z',
      'updatedAtUtc': '2026-11-27T09:00:00Z',
      'rowVersion': 'AAAAAAAAB9E=',
    });

    expect(v.status, VehicleStatus.underMaintenance);
    expect(v.currentLot!.code, 'TEST-A');
    expect(v.qrToken, 'q8Jd0abc');
    expect(v.rowVersion, 'AAAAAAAAB9E=');
    expect(v.ref.registrationNumber, 'D 01423');
  });

  test('dashboard parses figures, the 24 hourly counts and needs-attention lists', () {
    final d = Dashboard.fromJson({
      'onTripNow': 14,
      'transfersInTransit': 2,
      'tripsToday': 63,
      'secondsOutToday': 151200,
      'freeSpaces': 212,
      'tripsPerHourToday': [for (var h = 0; h < 24; h++) {'hour': h, 'trips': h == 9 ? 5 : 0}],
      'needsAttention': {
        'overdueTrips': 1,
        'overdueTripList': [
          {
            'id': 4,
            'status': 'Active',
            'startedAtUtc': '2026-11-27T05:00:00Z',
            'isOverdue': true,
            'originLot': _lotRef,
            'destination': {'locationId': 5, 'area': 'Deira', 'location': 'Fish Market'},
            'currentStaff': _staffRef,
            'currentVehicle': _vehicleRef,
            'segmentCount': 1,
          },
        ],
        'overdueTransfers': 0,
        'vehiclesNotAtLot': 1,
        'vehiclesNotAtLotList': [_vehicleRef],
        'lotsOverCapacity': [
          {'lot': _lotRef, 'capacity': 60, 'occupied': 61},
        ],
        'transfersWaiting': 0,
        'transfersWaitingList': <Object>[],
      },
    });

    expect(d.tripsPerHourToday, hasLength(24));
    expect(d.tripsPerHourToday[9].trips, 5);
    expect(d.needsAttention.overdueTripList.single.isOverdue, isTrue);
    expect(d.needsAttention.lotsOverCapacity.single.occupied, 61);
    expect(d.needsAttention.isEmpty, isFalse);
  });

  test('lot input sends no coordinates (D-056) and upper-cases the code', () {
    const input = LotInput(
      code: 'aqd',
      name: ' Al Qusais Depot ',
      address: 'Street 1',
      contactName: 'Imran',
      contactNumber: '+971500000001',
      lotType: LotType.underground,
      totalCapacity: 80,
      baselineOccupied: 5,
      typeCapacities: [TypeCapacityInput(vehicleTypeId: 2, capacity: 20, baselineOccupied: 1)],
    );

    final json = input.toJson();

    expect(json.containsKey('latitude'), isFalse);
    expect(json.containsKey('longitude'), isFalse);
    expect(json['code'], 'AQD');
    expect(json['name'], 'Al Qusais Depot');
    expect(json['lotType'], 'Underground');
    expect(json['opensAt'], isNull);
    expect(json['typeCapacities'], [
      {'vehicleTypeId': 2, 'capacity': 20, 'baselineOccupied': 1},
    ]);
  });

  test('lot reads admin fields: active flag, baseline per type and rowVersion', () {
    final lot = Lot.fromJson({
      'id': 1,
      'code': 'TEST-A',
      'name': 'Test Lot A',
      'address': 'Somewhere',
      'contactName': 'Test',
      'contactNumber': '+971500000001',
      'opensAt': null,
      'closesAt': null,
      'lotType': 'Open',
      'isActive': false,
      'baselineOccupied': 3,
      'total': {'capacity': 50, 'occupied': 10, 'available': 40, 'isFull': false, 'isOverCapacity': false},
      'byType': [
        {'vehicleTypeId': 2, 'type': 'Sedan', 'category': 'Car', 'capacity': 20, 'baselineOccupied': 2, 'occupied': 4, 'available': 16},
      ],
      'rowVersion': 'AAAAAAAAB9E=',
    });

    expect(lot.isActive, isFalse);
    expect(lot.baselineOccupied, 3);
    expect(lot.byType.single.baselineOccupied, 2);
    expect(lot.rowVersion, 'AAAAAAAAB9E=');
    expect(lot.hours, 'Open 24 hours');
  });

  test('audit entry keeps before/after field maps', () {
    final entry = AuditEntry.fromJson({
      'id': 9,
      'atUtc': '2026-11-27T10:00:00Z',
      'user': {'id': 1, 'loginId': 'ADM2600001', 'fullName': 'Admin User'},
      'action': 'StatusChange',
      'entityType': 'Vehicle',
      'entityId': 12,
      'before': {'Status': 'Available'},
      'after': {'Status': 'UnderMaintenance'},
    });

    expect(entry.user.loginId, 'ADM2600001');
    expect(entry.before!['Status'], 'Available');
    expect(entry.after!['Status'], 'UnderMaintenance');
  });

  test('staff hours report parses rows and per-day seconds', () {
    final report = StaffHoursReport.fromJson({
      'totalSeconds': 7200,
      'staff': 1,
      'rows': [
        {
          'staff': _staffRef,
          'trips': 1,
          'transfers': 0,
          'tripSeconds': 7200,
          'transferSeconds': 0,
          'totalSeconds': 7200,
          'days': [
            {'date': '2026-11-14', 'seconds': 3600},
            {'date': '2026-11-15', 'seconds': 3600},
          ],
        },
      ],
    });

    expect(report.rows.single.days.map((d) => d.date), ['2026-11-14', '2026-11-15']);
    expect(report.rows.single.staff.loginId, 'DM2600001');
  });

  test('a stale rowVersion is recognised as a conflict', () {
    const conflict = ApiException(status: 409, code: 'CONCURRENCY_CONFLICT', title: 'Changed', detail: 'Reload.');
    const other = ApiException(status: 409, code: 'DUPLICATE_VALUE', title: 'Duplicate', detail: 'Exists.');

    expect(conflict.isConflict, isTrue);
    expect(other.isConflict, isFalse);
  });
}
