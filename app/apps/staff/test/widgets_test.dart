import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:staff/features/home.dart';
import 'package:staff/features/trips.dart';
import 'package:staff/state.dart';

class _FakeSession extends SessionController {
  _FakeSession(this.initial, {this.loginError});

  final SessionState initial;
  final ApiException? loginError;

  @override
  SessionState build() => initial;

  @override
  Future<void> login(String loginId, String password) async {
    if (loginError != null) {
      throw loginError!;
    }
  }
}

class _FakeActiveTrip extends ActiveTripController {
  _FakeActiveTrip(this.trip);

  final TripDetail? trip;

  @override
  Future<TripDetail?> build() async => trip;
}

const _me = Me(
  id: 7,
  loginId: 'DM2600001',
  role: UserRole.staff,
  fullName: 'Test Driver One',
  mobile: '+971500000011',
  thisMonth: MonthStats(trips: 3, totalSeconds: 5400),
);

TripDetail _trip({required bool overdue}) => TripDetail.fromJson({
  'id': 42,
  'status': 'Active',
  'startedAtUtc': DateTime.now().toUtc().subtract(const Duration(hours: 3)).toIso8601String(),
  'isOverdue': overdue,
  'nextReminderAtUtc': DateTime.now().toUtc().add(const Duration(minutes: 30)).toIso8601String(),
  'originLot': {'id': 1, 'code': 'TEST-A', 'name': 'Test Lot A'},
  'destination': {'locationId': 5, 'area': 'Test Downtown', 'location': 'Test Mall Entrance'},
  'currentStaff': {'id': 7, 'loginId': 'DM2600001', 'fullName': 'Test Driver One'},
  'currentVehicle': {
    'id': 3,
    'registrationNumber': 'TEST-1001',
    'category': 'Test Car',
    'type': 'Test Sedan',
    'brand': 'Toyota',
    'model': 'Camry',
    'colour': 'White',
    'status': 'Active',
  },
  'segments': <Object>[],
});

Future<void> _pumpHome(WidgetTester tester, TripDetail? trip) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        sessionProvider.overrideWith(() => _FakeSession(const SignedIn(_me))),
        activeTripProvider.overrideWith(() => _FakeActiveTrip(trip)),
        transferTasksProvider.overrideWith((ref) async => const TransferTasks(awaitingDeparture: [], inTransit: [])),
        lotsProvider.overrideWith((ref) async => const <Lot>[]),
        unreadCountProvider.overrideWith((ref) async => 0),
      ],
      child: MaterialApp(theme: AppTheme.light(), home: const Scaffold(body: HomeScreen())),
    ),
  );
  await tester.pump();
  await tester.pump();
}

/// Removes the tree so the per-second timers stop before the test ends.
Future<void> _unmount(WidgetTester tester) => tester.pumpWidget(const SizedBox());

void main() {
  testWidgets('login asks for ID and password before calling the server', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          appRoleProvider.overrideWithValue(UserRole.staff),
          sessionProvider.overrideWith(() => _FakeSession(const SignedOut())),
        ],
        child: MaterialApp(theme: AppTheme.light(), home: const LoginScreen()),
      ),
    );

    await tester.ensureVisible(find.byKey(const Key('signIn')));
    await tester.tap(find.byKey(const Key('signIn')));
    await tester.pump();

    expect(find.text('Enter your Staff ID.'), findsOneWidget);
    expect(find.text('Enter your password.'), findsOneWidget);
  });

  testWidgets('login shows the server message for a wrong password', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          appRoleProvider.overrideWithValue(UserRole.staff),
          sessionProvider.overrideWith(
            () => _FakeSession(
              const SignedOut(),
              loginError: const ApiException(
                status: 401,
                code: 'INVALID_CREDENTIALS',
                title: 'Sign-in failed',
                detail: 'The ID or password is incorrect.',
              ),
            ),
          ),
        ],
        child: MaterialApp(theme: AppTheme.light(), home: const LoginScreen()),
      ),
    );

    await tester.enterText(find.byKey(const Key('loginId')), 'DM2600001');
    await tester.enterText(find.byKey(const Key('password')), 'wrong-password');
    await tester.ensureVisible(find.byKey(const Key('signIn')));
    await tester.tap(find.byKey(const Key('signIn')));
    await tester.pump();
    await tester.pump();

    expect(find.text('The ID or password is incorrect.'), findsOneWidget);
  });

  testWidgets('home without a trip offers to scan and shows this month', (tester) async {
    await _pumpHome(tester, null);

    expect(find.text('Ready for the next vehicle'), findsOneWidget);
    expect(find.text('Scan to start trip'), findsOneWidget);
    expect(find.text('1h 30m'), findsOneWidget);
    await _unmount(tester);
  });

  testWidgets('home with a running trip shows the timer hero and trip actions', (tester) async {
    await _pumpHome(tester, _trip(overdue: false));

    expect(find.text('Live'), findsOneWidget);
    expect(find.text('TEST-1001'), findsOneWidget);
    expect(find.text('End trip'), findsOneWidget);
    expect(find.text('Swap'), findsOneWidget);
    expect(find.textContaining('Reminder at'), findsOneWidget);
    await _unmount(tester);
  });

  testWidgets('home marks an overdue trip', (tester) async {
    await _pumpHome(tester, _trip(overdue: true));

    expect(find.text('Overdue'), findsOneWidget);
    expect(find.textContaining('Past the reminder time'), findsOneWidget);
    await _unmount(tester);
  });

  testWidgets('my trips lists a trip I handed over (still running under someone else)', (tester) async {
    final handedOver = TripSummary.fromJson({
      'id': 4,
      'status': 'Active',
      'startedAtUtc': DateTime.now().toUtc().subtract(const Duration(minutes: 5)).toIso8601String(),
      'originLot': {'id': 1, 'code': 'TEST-A', 'name': 'Test Lot A'},
      'destination': {'locationId': 5, 'area': 'Test Downtown', 'location': 'Test Mall Entrance'},
      'currentStaff': {'id': 8, 'loginId': 'DM2600002', 'fullName': 'Test Driver Two'},
      'currentVehicle': {'id': 5, 'registrationNumber': 'TEST-1003', 'category': 'Test Car', 'type': 'Test Sedan'},
      'segmentCount': 2,
    });
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          sessionProvider.overrideWith(() => _FakeSession(const SignedIn(_me))),
          activeTripProvider.overrideWith(() => _FakeActiveTrip(null)),
          tripsProvider(TripRange.today).overrideWith((ref) async => [handedOver]),
        ],
        child: MaterialApp(theme: AppTheme.light(), home: const Scaffold(body: TripsScreen())),
      ),
    );
    await tester.pump();
    await tester.pump();

    expect(find.text('TEST-1003'), findsOneWidget);
    expect(find.text('Handed over'), findsOneWidget);
  });
}
