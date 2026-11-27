import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../state.dart';
import 'common.dart';

/// S-24: details, driving licence, this month's totals, theme, change password, log out.
/// No assigned lots: staff aren't tied to lots (D-047).
class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final me = ref.watch(meProvider);
    final themeMode = ref.watch(themeModeProvider);
    final c = context.dm;
    final month = me.thisMonth;
    return RefreshIndicator(
      onRefresh: () => ref.read(sessionProvider.notifier).refreshMe(),
      child: ListView(
        padding: const EdgeInsets.fromLTRB(DmSize.gutter, 12, DmSize.gutter, 24),
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
                      Text('Staff · driver · ${me.loginId}', style: dmStyle(13, FontWeight.w600, color: Colors.white70)),
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
                KeyValueRow('Driving licence', me.licenceNumber ?? '—'),
              ],
            ),
          ),
          if (month != null) ...[
            const SectionLabel('This month'),
            AppCard(
              child: Column(
                children: [
                  KeyValueRow('Trips', '${month.trips}'),
                  KeyValueRow('Time out', formatDuration(Duration(seconds: month.totalSeconds))),
                ],
              ),
            ),
          ],
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
              final sure = await showDialog<bool>(
                context: context,
                builder: (context) => AlertDialog(
                  title: const Text('Log out?'),
                  actions: [
                    TextButton(onPressed: () => Navigator.of(context).pop(false), child: const Text('Stay')),
                    TextButton(onPressed: () => Navigator.of(context).pop(true), child: const Text('Log out')),
                  ],
                ),
              );
              if (sure ?? false) {
                await ref.read(sessionProvider.notifier).logout();
              }
            },
            icon: const Icon(Icons.logout_rounded),
            label: const Text('Log out'),
          ),
          const SizedBox(height: 12),
          Center(child: Text('DM Staff ${AppConfig.appVersion}', style: context.text.labelSmall)),
        ],
      ),
    );
  }
}

/// C-06: current password, new password twice. Other devices are signed out (AUTH-07).
class ChangePasswordScreen extends ConsumerStatefulWidget {
  const ChangePasswordScreen({super.key});

  @override
  ConsumerState<ChangePasswordScreen> createState() => _ChangePasswordScreenState();
}

class _ChangePasswordScreenState extends ConsumerState<ChangePasswordScreen> {
  final _form = GlobalKey<FormState>();
  final _current = TextEditingController();
  final _new = TextEditingController();
  final _repeat = TextEditingController();
  ApiException? _error;

  @override
  void dispose() {
    _current.dispose();
    _new.dispose();
    _repeat.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() => _error = null);
    if (!_form.currentState!.validate()) {
      return;
    }
    try {
      await ref.read(reposProvider).changePassword(_current.text, _new.text);
      if (mounted) {
        showMessage(context, 'Password changed. Other devices are signed out.');
        context.pop();
      }
    } on ApiException catch (error) {
      if (mounted) {
        setState(() => _error = error);
        if (error.fieldErrors.isEmpty) {
          showError(context, error);
        }
      }
    }
  }

  @override
  Widget build(BuildContext context) => FlowScaffold(
    title: 'Change password',
    bottom: BusyButton(label: 'Change password', icon: Icons.check_rounded, onPressed: _submit),
    children: [
      Form(
        key: _form,
        child: Column(
          children: [
            TextFormField(
              controller: _current,
              obscureText: true,
              decoration: InputDecoration(labelText: 'Current password', errorText: _error?.fieldError('currentPassword')),
              validator: (value) => (value ?? '').isEmpty ? 'Enter your current password.' : null,
            ),
            const SizedBox(height: 14),
            TextFormField(
              controller: _new,
              obscureText: true,
              decoration: InputDecoration(
                labelText: 'New password',
                helperText: '8 to 128 characters',
                errorText: _error?.fieldError('newPassword'),
              ),
              validator: (value) {
                final length = (value ?? '').length;
                return length < 8 || length > 128 ? 'Use 8 to 128 characters.' : null;
              },
            ),
            const SizedBox(height: 14),
            TextFormField(
              controller: _repeat,
              obscureText: true,
              decoration: const InputDecoration(labelText: 'Repeat new password'),
              validator: (value) => value != _new.text ? 'The passwords don’t match.' : null,
            ),
          ],
        ),
      ),
    ],
  );
}
