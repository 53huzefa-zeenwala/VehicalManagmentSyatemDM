import 'package:admin/features/dashboard.dart';
import 'package:admin/features/qr_labels.dart';
import 'package:admin/features/vehicles.dart';
import 'package:admin/state.dart';
import 'package:admin/widgets/admin_widgets.dart';
import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

class _FakeSession extends SessionController {
  _FakeSession(this.initial);

  final SessionState initial;

  @override
  SessionState build() => initial;
}

const _me = Me(id: 1, loginId: 'ADM2600001', role: UserRole.admin, fullName: 'Admin User', mobile: '+971500000001');

Dashboard _dashboard({bool attention = true}) => Dashboard.fromJson({
  'onTripNow': 3,
  'transfersInTransit': 1,
  'tripsToday': 12,
  'secondsOutToday': 5400,
  'freeSpaces': 47,
  'tripsPerHourToday': [for (var h = 0; h < 24; h++) {'hour': h, 'trips': h == 8 ? 4 : 0}],
  'needsAttention': {
    'overdueTrips': 0,
    'overdueTripList': <Object>[],
    'overdueTransfers': 0,
    'vehiclesNotAtLot': attention ? 1 : 0,
    'vehiclesNotAtLotList': [
      if (attention) {'id': 3, 'registrationNumber': 'TEST-1003', 'category': 'Test Car', 'type': 'Test Sedan'},
    ],
    'lotsOverCapacity': <Object>[],
    'transfersWaiting': 0,
    'transfersWaitingList': <Object>[],
  },
});

Widget _app(Widget child, {List<Override> overrides = const []}) => ProviderScope(
  overrides: [
    appRoleProvider.overrideWithValue(UserRole.admin),
    sessionProvider.overrideWith(() => _FakeSession(const SignedIn(_me))),
    unreadCountProvider.overrideWith((ref) async => 2),
    ...overrides,
  ],
  child: MaterialApp(theme: AppTheme.light(), home: child),
);

void main() {
  testWidgets('login screen speaks to admins', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          appRoleProvider.overrideWithValue(UserRole.admin),
          sessionProvider.overrideWith(() => _FakeSession(const SignedOut())),
        ],
        child: MaterialApp(theme: AppTheme.light(), home: const LoginScreen()),
      ),
    );

    expect(find.text('Admin ID'), findsOneWidget);
    await tester.ensureVisible(find.byKey(const Key('signIn')));
    await tester.tap(find.byKey(const Key('signIn')));
    await tester.pump();
    expect(find.text('Enter your Admin ID.'), findsOneWidget);
  });

  testWidgets('dashboard shows today, the bell badge and what needs attention', (tester) async {
    await tester.pumpWidget(
      _app(const DashboardScreen(), overrides: [dashboardProvider.overrideWith((ref) async => _dashboard())]),
    );
    await tester.pump();
    await tester.pump();

    expect(find.text('On a trip now'), findsOneWidget);
    expect(find.text('3'), findsOneWidget);
    expect(find.text('12'), findsOneWidget);
    expect(find.text('1h 30m'), findsOneWidget);
    expect(find.text('2'), findsOneWidget); // unread badge
    await tester.scrollUntilVisible(find.text('1 vehicle not at a lot'), 200, scrollable: find.byType(Scrollable).first);
    expect(find.text('1 vehicle not at a lot'), findsOneWidget);
    expect(find.text('TEST-1003'), findsOneWidget);
  });

  testWidgets('dashboard says all clear when nothing needs attention', (tester) async {
    await tester.pumpWidget(
      _app(const DashboardScreen(), overrides: [dashboardProvider.overrideWith((ref) async => _dashboard(attention: false))]),
    );
    await tester.pump();
    await tester.pump();
    await tester.scrollUntilVisible(find.text('All clear'), 200, scrollable: find.byType(Scrollable).first);

    expect(find.text('All clear'), findsOneWidget);
  });

  testWidgets('vehicle form checks required fields before calling the server', (tester) async {
    await tester.pumpWidget(
      _app(
        const VehicleFormScreen(),
        overrides: [
          activeTypeOptionsProvider.overrideWith(
            (ref) async => const [TypeOption(id: 2, categoryId: 1, label: 'Test Car · Test Sedan')],
          ),
          activeLotsProvider.overrideWith((ref) async => const <Lot>[]),
        ],
      ),
    );
    await tester.pump();
    await tester.pump();

    final button = find.ancestor(of: find.text('Add vehicle'), matching: find.byWidgetPredicate((w) => w is FilledButton));
    await tester.ensureVisible(button);
    await tester.tap(button);
    await tester.pump();
    await tester.pump();

    expect(find.text('Enter the registration number.'), findsOneWidget);
    expect(find.text('Pick a vehicle type.'), findsOneWidget);
    expect(find.text('Enter the brand.'), findsOneWidget);
  });

  testWidgets('a stale record asks to reload', (tester) async {
    var reloaded = false;
    await tester.pumpWidget(
      MaterialApp(
        home: Builder(
          builder: (context) => TextButton(
            onPressed: () => showSaveError(
              context,
              const ApiException(status: 409, code: 'CONCURRENCY_CONFLICT', title: 'Changed', detail: 'Reload.'),
              onReload: () => reloaded = true,
            ),
            child: const Text('save'),
          ),
        ),
      ),
    );

    await tester.tap(find.text('save'));
    await tester.pumpAndSettle();
    expect(find.text('Changed by someone else'), findsOneWidget);

    await tester.tap(find.text('Reload'));
    await tester.pumpAndSettle();
    expect(reloaded, isTrue);
  });

  testWidgets('a QR label renders to a 50 × 70 mm PNG at 300 dpi', (tester) async {
    const label = QrLabel(
      vehicleId: 3,
      qrToken: 'q8Jd0abcdefghijklmnopq',
      registrationNumber: 'TEST-1001',
      category: 'Test Car',
      type: 'Test Sedan',
    );

    final png = (await tester.runAsync(() => renderLabelPng(label)))!;

    // PNG signature, then the IHDR width and height (big-endian) at bytes 16–23.
    expect(png.sublist(0, 8), [137, 80, 78, 71, 13, 10, 26, 10]);
    final width = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
    final height = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
    expect((width, height), (591, 827));
  });

  test('date range labels and limits', () {
    final range = DateRange(DateTime(2026, 11, 1), DateTime(2026, 11, 27));
    expect(range.label, '1 Nov – 27 Nov 2026');
    expect(DateRange.lastDays(7).to.difference(DateRange.lastDays(7).from).inDays, 6);
  });
}
