import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import 'features/scan.dart';
import 'state.dart';

/// Tab scaffold: live trip bar on top, tab bar with the raised centre Scan button below (D-022).
class StaffShell extends ConsumerWidget {
  const StaffShell({super.key, required this.shell});

  final StatefulNavigationShell shell;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final trip = ref.watch(activeTripProvider).valueOrNull;
    return Scaffold(
      body: SafeArea(
        bottom: false,
        child: Column(
          children: [
            if (trip != null) LiveBar(trip: trip),
            Expanded(child: shell),
          ],
        ),
      ),
      bottomNavigationBar: _TabBar(
        index: shell.currentIndex,
        onTab: (index) => shell.goBranch(index, initialLocation: index == shell.currentIndex),
        onScan: () => ScanFlow.fromTabBar(context, ref),
      ),
    );
  }
}

/// Amber bar while a trip runs (red when overdue): plate and elapsed time; tap opens the trip.
class LiveBar extends StatelessWidget {
  const LiveBar({super.key, required this.trip});

  final TripDetail trip;

  @override
  Widget build(BuildContext context) {
    final c = context.dm;
    final overdue = trip.isOverdue;
    final background = overdue ? c.danger : c.accent;
    final foreground = overdue ? Colors.white : c.onAccent;
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 4),
      child: Material(
        color: background,
        borderRadius: BorderRadius.circular(16),
        child: InkWell(
          borderRadius: BorderRadius.circular(16),
          onTap: () => context.push('/trip'),
          child: SizedBox(
            height: 46,
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 14),
              child: Row(
                children: [
                  Icon(overdue ? Icons.warning_rounded : Icons.radio_button_checked_rounded, size: 18, color: foreground),
                  const SizedBox(width: 8),
                  Text(overdue ? 'Overdue' : 'Trip running', style: dmStyle(13.5, FontWeight.w700, color: foreground)),
                  const SizedBox(width: 8),
                  Flexible(
                    child: Text(
                      trip.currentVehicle.registrationNumber,
                      overflow: TextOverflow.ellipsis,
                      style: dmStyle(13.5, FontWeight.w700, color: foreground),
                    ),
                  ),
                  const Spacer(),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                    decoration: BoxDecoration(
                      color: foreground.withValues(alpha: .12),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Ticker(
                      builder: (_, now) => Text(
                        formatClock(now.difference(trip.startedAtUtc)),
                        style: dmStyle(15, FontWeight.w800, color: foreground, tabular: true),
                      ),
                    ),
                  ),
                  Icon(Icons.chevron_right_rounded, color: foreground),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _TabBar extends StatelessWidget {
  const _TabBar({required this.index, required this.onTab, required this.onScan});

  final int index;
  final ValueChanged<int> onTab;
  final VoidCallback onScan;

  @override
  Widget build(BuildContext context) {
    final c = context.dm;
    // Outline icon normally, filled when the tab is active (DESIGN_SYSTEM.md tab bar).
    Widget tab(int i, IconData icon, IconData activeIcon, String label) {
      final active = index == i;
      final color = active ? c.primary : c.text3;
      return Expanded(
        child: InkResponse(
          onTap: () => onTab(i),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(active ? activeIcon : icon, color: color, size: 24),
              const SizedBox(height: 3),
              Text(label, style: dmStyle(11, FontWeight.w600, color: color)),
            ],
          ),
        ),
      );
    }

    return DecoratedBox(
      decoration: BoxDecoration(
        color: c.chrome,
        border: Border(top: BorderSide(color: c.border)),
        boxShadow: c.isDark ? null : const [BoxShadow(color: Color(0x140D1B1E), blurRadius: 16, offset: Offset(0, -4))],
      ),
      child: SafeArea(
        top: false,
        child: SizedBox(
          height: 68,
          child: Row(
            children: [
              tab(0, Icons.home_outlined, Icons.home_rounded, 'Home'),
              tab(1, Icons.route_outlined, Icons.route_rounded, 'Trips'),
              SizedBox(
                width: 84,
                child: Transform.translate(
                  offset: const Offset(0, -16),
                  child: Center(
                    child: Semantics(
                      button: true,
                      label: 'Scan',
                      child: GestureDetector(
                        onTap: onScan,
                        child: Container(
                          width: 62,
                          height: 62,
                          decoration: BoxDecoration(
                            color: c.primary,
                            borderRadius: BorderRadius.circular(22),
                            border: Border.all(color: c.bg, width: 5),
                            boxShadow: c.primaryGlow,
                          ),
                          child: Icon(Icons.qr_code_scanner_rounded, color: c.onPrimary, size: 30),
                        ),
                      ),
                    ),
                  ),
                ),
              ),
              tab(2, Icons.local_parking_outlined, Icons.local_parking_rounded, 'Lots'),
              tab(3, Icons.person_outline_rounded, Icons.person_rounded, 'Profile'),
            ],
          ),
        ),
      ),
    );
  }
}
