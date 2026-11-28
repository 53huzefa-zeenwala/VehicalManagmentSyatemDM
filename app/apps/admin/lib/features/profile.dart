import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

/// C-05: the admin's details, theme, change password, log out.
class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final me = ref.watch(meProvider);
    final themeMode = ref.watch(themeModeProvider);
    final c = context.dm;
    return Scaffold(
      appBar: AppBar(title: const Text('Profile')),
      body: RefreshIndicator(
        onRefresh: () => ref.read(sessionProvider.notifier).refreshMe(),
        child: ListView(
          padding: const EdgeInsets.fromLTRB(DmSize.gutter, 8, DmSize.gutter, 24),
          children: [
            Container(
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(gradient: c.hero, borderRadius: BorderRadius.circular(DmSize.rHero)),
              child: Row(
                children: [
                  Avatar(name: me.fullName, photoUrl: me.photoUrl, size: 60),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(me.fullName, style: dmStyle(20, FontWeight.w800, color: Colors.white)),
                        Text('Admin · ${me.loginId}', style: dmStyle(13, FontWeight.w600, color: Colors.white70)),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: DmSize.gap),
            AppCard(
              child: Column(
                children: [
                  KeyValueRow('Mobile', me.mobile),
                  if (me.email != null) KeyValueRow('Email', me.email!),
                ],
              ),
            ),
            const SectionLabel('Theme'),
            SegmentedButton<ThemeMode>(
              showSelectedIcon: false,
              segments: const [
                ButtonSegment(value: ThemeMode.system, label: Text('System')),
                ButtonSegment(value: ThemeMode.light, label: Text('Light')),
                ButtonSegment(value: ThemeMode.dark, label: Text('Dark')),
              ],
              selected: {themeMode},
              onSelectionChanged: (value) => ref.read(themeModeProvider.notifier).state = value.first,
            ),
            const SizedBox(height: 18),
            OutlinedButton.icon(
              onPressed: () => context.push('/profile/password'),
              icon: const Icon(Icons.lock_rounded),
              label: const Text('Change password'),
            ),
            const SizedBox(height: 10),
            OutlinedButton.icon(
              style: OutlinedButton.styleFrom(foregroundColor: c.dText),
              onPressed: () async {
                if (await confirmAction(context, title: 'Log out?', message: 'You can sign in again any time.', confirmLabel: 'Log out', cancelLabel: 'Stay')) {
                  await ref.read(sessionProvider.notifier).logout();
                }
              },
              icon: const Icon(Icons.logout_rounded),
              label: const Text('Log out'),
            ),
            const SizedBox(height: 12),
            Center(child: Text('DM Admin ${AppConfig.appVersion}', style: context.text.labelSmall)),
          ],
        ),
      ),
    );
  }
}
