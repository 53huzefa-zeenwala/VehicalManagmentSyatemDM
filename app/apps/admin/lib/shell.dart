import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

/// Admin tab scaffold (D-022): Dashboard · Live · Manage · Reports, no Scan (admins don't scan, D-021).
class AdminShell extends StatelessWidget {
  const AdminShell({super.key, required this.shell});

  final StatefulNavigationShell shell;

  @override
  Widget build(BuildContext context) => Scaffold(
    body: shell,
    bottomNavigationBar: NavigationBar(
      selectedIndex: shell.currentIndex,
      onDestinationSelected: (index) => shell.goBranch(index, initialLocation: index == shell.currentIndex),
      destinations: const [
        NavigationDestination(icon: Icon(Icons.space_dashboard_outlined), selectedIcon: Icon(Icons.space_dashboard_rounded), label: 'Dashboard'),
        NavigationDestination(icon: Icon(Icons.sensors_outlined), selectedIcon: Icon(Icons.sensors_rounded), label: 'Live'),
        NavigationDestination(icon: Icon(Icons.tune_outlined), selectedIcon: Icon(Icons.tune_rounded), label: 'Manage'),
        NavigationDestination(icon: Icon(Icons.insert_chart_outlined_rounded), selectedIcon: Icon(Icons.insert_chart_rounded), label: 'Reports'),
      ],
    ),
  );
}

/// Top bar of every tab: title, the notifications bell with its badge, and the avatar that opens the profile (C-05).
class TabAppBar extends ConsumerWidget implements PreferredSizeWidget {
  const TabAppBar({super.key, required this.title, this.bottom});

  final String title;
  final PreferredSizeWidget? bottom;

  @override
  Size get preferredSize => Size.fromHeight(kToolbarHeight + (bottom?.preferredSize.height ?? 0));

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final me = ref.watch(meProvider);
    return AppBar(
      title: Text(title),
      bottom: bottom,
      actions: [
        NotificationBell(onPressed: () => context.push('/notifications')),
        Padding(
          padding: const EdgeInsets.only(right: 12, left: 4),
          child: Semantics(
            button: true,
            label: 'Profile',
            child: InkWell(
              customBorder: const CircleBorder(),
              onTap: () => context.push('/profile'),
              child: Avatar(name: me.fullName, photoUrl: me.photoUrl, size: 34),
            ),
          ),
        ),
      ],
    );
  }
}
