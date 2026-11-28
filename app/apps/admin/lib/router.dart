import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import 'features/accounts.dart';
import 'features/audit.dart';
import 'features/categories.dart';
import 'features/dashboard.dart';
import 'features/destinations.dart';
import 'features/live.dart';
import 'features/lots.dart';
import 'features/manage.dart';
import 'features/profile.dart';
import 'features/qr_labels.dart';
import 'features/reports.dart';
import 'features/settings.dart';
import 'features/staff.dart';
import 'features/transfers.dart';
import 'features/trip_detail.dart';
import 'features/vehicles.dart';
import 'shell.dart';

int _id(GoRouterState state) => int.parse(state.pathParameters['id']!);

/// Admin navigation (UX_ARCHITECTURE.md §3, D-022): tabs Dashboard · Live · Manage · Reports; details and forms
/// are pushed on top.
final routerProvider = Provider<GoRouter>((ref) {
  final session = ValueNotifier<SessionState>(ref.read(sessionProvider));
  ref
    ..listen(sessionProvider, (_, next) => session.value = next)
    ..onDispose(session.dispose);

  return GoRouter(
    initialLocation: '/dashboard',
    refreshListenable: session,
    redirect: (context, state) {
      final location = state.matchedLocation;
      return switch (session.value) {
        SessionChecking() => location == '/splash' ? null : '/splash',
        SignedOut() => location == '/login' ? null : '/login',
        SignedIn() => (location == '/login' || location == '/splash') ? '/dashboard' : null,
      };
    },
    routes: [
      GoRoute(path: '/splash', builder: (_, _) => const _Splash()),
      GoRoute(path: '/login', builder: (_, _) => const LoginScreen()),
      StatefulShellRoute.indexedStack(
        builder: (_, _, shell) => AdminShell(shell: shell),
        branches: [
          StatefulShellBranch(routes: [GoRoute(path: '/dashboard', builder: (_, _) => const DashboardScreen())]),
          StatefulShellBranch(routes: [GoRoute(path: '/live', builder: (_, _) => const LiveScreen())]),
          StatefulShellBranch(routes: [GoRoute(path: '/manage', builder: (_, _) => const ManageScreen())]),
          StatefulShellBranch(routes: [GoRoute(path: '/reports', builder: (_, _) => const ReportsHubScreen())]),
        ],
      ),
      GoRoute(
        path: '/notifications',
        builder: (_, _) => NotificationsScreen(
          onOpen: (context, ref, item) async {
            final path = switch (item.targetType) {
              'Trip' => '/trips/${item.targetId}',
              'Transfer' => '/transfers/${item.targetId}',
              _ => null,
            };
            if (path != null) {
              await context.push(path);
            }
          },
        ),
      ),
      GoRoute(path: '/profile', builder: (_, _) => const ProfileScreen()),
      GoRoute(path: '/profile/password', builder: (_, _) => const ChangePasswordScreen()),
      GoRoute(path: '/trips/:id', builder: (_, state) => TripDetailScreen(id: _id(state))),
      GoRoute(
        path: '/transfers/new',
        builder: (_, state) => NewTransferScreen(vehicle: state.extra is VehicleDetail ? state.extra! as VehicleDetail : null),
      ),
      GoRoute(path: '/transfers/:id', builder: (_, state) => TransferDetailScreen(id: _id(state))),
      // Fleet
      GoRoute(path: '/vehicles', builder: (_, state) => VehiclesScreen(notAtLot: state.uri.queryParameters['notAtLot'] == 'true')),
      GoRoute(path: '/vehicles/new', builder: (_, _) => const VehicleFormScreen()),
      GoRoute(path: '/vehicles/:id', builder: (_, state) => VehicleDetailScreen(id: _id(state))),
      GoRoute(path: '/vehicles/:id/edit', builder: (_, state) => VehicleFormScreen(id: _id(state))),
      GoRoute(path: '/vehicles/:id/label', builder: (_, state) => QrLabelScreen(vehicleIds: [_id(state)])),
      GoRoute(path: '/qr-labels', builder: (_, _) => const BulkQrScreen()),
      GoRoute(path: '/categories', builder: (_, _) => const CategoriesScreen()),
      // People
      GoRoute(path: '/staff', builder: (_, _) => const StaffListScreen()),
      GoRoute(path: '/staff/new', builder: (_, _) => const StaffFormScreen()),
      GoRoute(path: '/staff/:id', builder: (_, state) => StaffDetailScreen(id: _id(state))),
      GoRoute(path: '/staff/:id/edit', builder: (_, state) => StaffFormScreen(id: _id(state))),
      GoRoute(path: '/admins', builder: (_, _) => const AdminsScreen()),
      GoRoute(path: '/admins/new', builder: (_, _) => const AdminFormScreen()),
      GoRoute(path: '/admins/:id/edit', builder: (_, state) => AdminFormScreen(id: _id(state))),
      // Places and system
      GoRoute(path: '/lots', builder: (_, _) => const LotsScreen()),
      GoRoute(path: '/lots/new', builder: (_, _) => const LotFormScreen()),
      GoRoute(path: '/lots/:id', builder: (_, state) => LotDetailScreen(id: _id(state))),
      GoRoute(path: '/lots/:id/edit', builder: (_, state) => LotFormScreen(id: _id(state))),
      GoRoute(path: '/destinations', builder: (_, _) => const DestinationsScreen()),
      GoRoute(path: '/settings', builder: (_, _) => const SettingsScreen()),
      GoRoute(path: '/audit', builder: (_, _) => const AuditLogScreen()),
      // Reports
      GoRoute(path: '/reports/vehicle-hours', builder: (_, _) => const VehicleHoursScreen()),
      GoRoute(path: '/reports/staff-hours', builder: (_, _) => const StaffHoursScreen()),
      GoRoute(path: '/reports/lot-utilization', builder: (_, _) => const LotUtilizationScreen()),
      GoRoute(path: '/reports/trip-log', builder: (_, _) => const TripLogScreen()),
      GoRoute(path: '/reports/transfers', builder: (_, _) => const TransferLogScreen()),
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
