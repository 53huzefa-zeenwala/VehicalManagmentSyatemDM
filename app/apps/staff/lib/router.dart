import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import 'features/active_trip.dart';
import 'features/cancel_trip.dart';
import 'features/end_trip.dart';
import 'features/home.dart';
import 'features/login.dart';
import 'features/lots.dart';
import 'features/notifications.dart';
import 'features/profile.dart';
import 'features/scan.dart';
import 'features/start_trip.dart';
import 'features/swap.dart';
import 'features/transfer.dart';
import 'features/trips.dart';
import 'shell.dart';

/// Staff navigation (UX_ARCHITECTURE.md, D-022): tabs Home · Trips · [Scan] · Lots · Profile, flows pushed on top.
final routerProvider = Provider<GoRouter>((ref) {
  final session = ValueNotifier<SessionState>(ref.read(sessionProvider));
  ref
    ..listen(sessionProvider, (_, next) => session.value = next)
    ..onDispose(session.dispose);

  return GoRouter(
    initialLocation: '/home',
    refreshListenable: session,
    redirect: (context, state) {
      final location = state.matchedLocation;
      return switch (session.value) {
        SessionChecking() => location == '/splash' ? null : '/splash',
        SignedOut() => location == '/login' ? null : '/login',
        SignedIn() => (location == '/login' || location == '/splash') ? '/home' : null,
      };
    },
    routes: [
      GoRoute(path: '/splash', builder: (_, _) => const _Splash()),
      GoRoute(path: '/login', builder: (_, _) => const LoginScreen()),
      StatefulShellRoute.indexedStack(
        builder: (_, _, shell) => StaffShell(shell: shell),
        branches: [
          StatefulShellBranch(routes: [GoRoute(path: '/home', builder: (_, _) => const HomeScreen())]),
          StatefulShellBranch(routes: [GoRoute(path: '/trips', builder: (_, _) => const TripsScreen())]),
          StatefulShellBranch(routes: [GoRoute(path: '/lots', builder: (_, _) => const LotsScreen())]),
          StatefulShellBranch(routes: [GoRoute(path: '/profile', builder: (_, _) => const ProfileScreen())]),
        ],
      ),
      GoRoute(path: '/scan', builder: (_, state) => ScannerScreen(purpose: state.extra as String? ?? 'Scan vehicle')),
      GoRoute(
        path: '/start',
        // The scanned vehicle is passed in memory; if it is gone (router refresh), go back to Home.
        redirect: (_, state) => state.extra is StartTripArgs ? null : '/home',
        builder: (_, state) => StartTripScreen(args: state.extra! as StartTripArgs),
      ),
      GoRoute(path: '/trip', builder: (_, _) => const ActiveTripScreen()),
      GoRoute(path: '/trip/end', builder: (_, _) => const EndTripScreen()),
      GoRoute(path: '/trip/swap', builder: (_, _) => const SwapScreen()),
      GoRoute(path: '/trip/cancel', builder: (_, _) => const CancelTripScreen()),
      GoRoute(
        path: '/trip/summary/:id',
        builder: (_, state) => TripSummaryScreen(
          id: int.parse(state.pathParameters['id']!),
          warnings: state.extra is List<Notice> ? state.extra! as List<Notice> : const [],
        ),
      ),
      GoRoute(
        path: '/trips/:id',
        builder: (_, state) => TripDetailScreen(id: int.parse(state.pathParameters['id']!)),
      ),
      GoRoute(
        path: '/transfers/:id',
        builder: (_, state) => TransferScreen(id: int.parse(state.pathParameters['id']!)),
      ),
      GoRoute(path: '/notifications', builder: (_, _) => const NotificationsScreen()),
      GoRoute(path: '/profile/password', builder: (_, _) => const ChangePasswordScreen()),
    ],
  );
});

class _Splash extends StatelessWidget {
  const _Splash();

  @override
  Widget build(BuildContext context) => Scaffold(
    body: DecoratedBox(
      decoration: BoxDecoration(gradient: context.dm.hero),
      child: const Center(child: CircularProgressIndicator(color: Colors.white)),
    ),
  );
}
