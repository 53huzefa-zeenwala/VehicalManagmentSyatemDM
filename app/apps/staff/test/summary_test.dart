import 'dart:io';

import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:staff/features/end_trip.dart';
import 'package:staff/state.dart';

void main() {
  testWidgets('trip summary renders an ended trip with a swap and a signature', (tester) async {
    final api = ApiClient.create(
      origin: Uri.parse('https://localhost:7181'),
      appVersion: 'test',
      storageDirectory: Directory.systemTemp.createTempSync('dm_test'),
    );
    const staff = {'id': 7, 'loginId': 'DM2600001', 'fullName': 'Test Driver One'};
    final trip = TripDetail.fromJson({
      'id': 1,
      'status': 'Ended',
      'startedAtUtc': '2026-11-27T11:32:00.000Z',
      'endedAtUtc': '2026-11-27T11:35:00.000Z',
      'totalSeconds': 180,
      'isOverdue': false,
      'nextReminderAtUtc': null,
      'originLot': {'id': 1, 'code': 'TEST-A', 'name': 'Test Lot A'},
      'destination': {'locationId': 5, 'area': 'Test Downtown', 'location': 'Test Mall Entrance'},
      'currentStaff': staff,
      'currentVehicle': {
        'id': 5,
        'registrationNumber': 'TEST-1003',
        'category': 'Test Car',
        'type': 'Test Sedan',
        'brand': 'Honda',
        'model': 'Accord',
        'colour': 'Silver',
        'status': 'Available',
        'currentLot': {'id': 1, 'code': 'TEST-A', 'name': 'Test Lot A'},
      },
      'signatureUrl': '/api/v1/files/signatures/0123456789abcdef0123456789abcdef.png',
      'segments': [
        {
          'seq': 1,
          'kind': 'Start',
          'staff': staff,
          'vehicle': {'id': 3, 'registrationNumber': 'TEST-1001', 'category': 'Test Car', 'type': 'Test Sedan'},
          'startedAtUtc': '2026-11-27T11:32:00.000Z',
          'endedAtUtc': '2026-11-27T11:33:00.000Z',
        },
        {
          'seq': 2,
          'kind': 'VehicleSwap',
          'staff': staff,
          'vehicle': {'id': 5, 'registrationNumber': 'TEST-1003', 'category': 'Test Car', 'type': 'Test Sedan'},
          'startedAtUtc': '2026-11-27T11:33:00.000Z',
          'endedAtUtc': '2026-11-27T11:35:00.000Z',
          'reason': 'Flat tire',
        },
      ],
      'returnLot': {'id': 1, 'code': 'TEST-A', 'name': 'Test Lot A'},
      'warnings': <Object>[],
    });

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          apiClientProvider.overrideWithValue(api),
          tripDetailProvider(1).overrideWith((ref) async => trip),
        ],
        child: MaterialApp(theme: AppTheme.light(), home: const TripSummaryScreen(id: 1)),
      ),
    );
    await tester.pump();
    await tester.pump();

    expect(tester.takeException(), isNull);
    expect(find.text('3m'), findsWidgets);
    expect(find.text('Total time outside the lot'), findsOneWidget);
  });
}
