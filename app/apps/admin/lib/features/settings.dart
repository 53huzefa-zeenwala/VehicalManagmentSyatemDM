import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../state.dart';
import '../widgets/admin_widgets.dart';

/// A-21: Staff ID prefix (with a live example) and the reminder delay (SET-01). Repeat reminders every 30 min
/// are fixed (D-045).
class SettingsScreen extends ConsumerStatefulWidget {
  const SettingsScreen({super.key});

  @override
  ConsumerState<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends ConsumerState<SettingsScreen> {
  final _form = GlobalKey<FormState>();
  final _prefix = TextEditingController();
  final _delay = TextEditingController();
  AppSettings? _settings;
  ApiException? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final s = await ref.read(adminReposProvider).settings();
      if (mounted) {
        setState(() {
          _settings = s;
          _error = null;
          _prefix.text = s.staffIdPrefix;
          _delay.text = '${s.reminderDelayMinutes}';
        });
      }
    } on ApiException catch (error) {
      if (mounted) {
        setState(() => _error = error);
      }
    }
  }

  @override
  void dispose() {
    _prefix.dispose();
    _delay.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    setState(() => _error = null);
    if (!_form.currentState!.validate()) {
      return;
    }
    try {
      final saved = await ref
          .read(adminReposProvider)
          .updateSettings(
            staffIdPrefix: _prefix.text,
            reminderDelayMinutes: int.parse(_delay.text),
            rowVersion: _settings!.rowVersion,
          );
      ref.invalidate(settingsProvider);
      if (mounted) {
        setState(() => _settings = saved);
        showMessage(context, 'Settings saved.');
        context.pop();
      }
    } on ApiException catch (error) {
      if (mounted) {
        setState(() => _error = error);
        showSaveError(context, error, onReload: _load);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final settings = _settings;
    if (settings == null) {
      return Scaffold(
        appBar: AppBar(title: const Text('Settings')),
        body: _error == null ? const SkeletonList(count: 2) : ErrorView(error: _error!, onRetry: _load),
      );
    }
    final year = (Gst.today().year % 100).toString().padLeft(2, '0');
    final delay = int.tryParse(_delay.text);
    return FlowScaffold(
      title: 'Settings',
      bottom: BusyButton(label: 'Save', onPressed: _save),
      children: [
        Form(
          key: _form,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              FormSection(
                title: 'Staff IDs',
                children: [
                  TextFormField(
                    controller: _prefix,
                    textCapitalization: TextCapitalization.characters,
                    inputFormatters: [FilteringTextInputFormatter.allow(RegExp('[A-Za-z]')), LengthLimitingTextInputFormatter(5)],
                    onChanged: (_) => setState(() {}),
                    decoration: InputDecoration(
                      labelText: 'Prefix (1 to 5 letters)',
                      errorText: _error?.fieldError('staffIdPrefix'),
                      helperMaxLines: 3,
                      helperText: 'Next new staff: ${_prefix.text.toUpperCase()}${year}00001-style. Existing IDs don’t change.',
                    ),
                    validator: (v) {
                      final value = (v ?? '').trim();
                      if (!RegExp(r'^[A-Za-z]{1,5}$').hasMatch(value)) {
                        return 'Use 1 to 5 letters.';
                      }
                      return value.toUpperCase() == 'ADM' ? 'ADM is reserved for admins.' : null;
                    },
                  ),
                ],
              ),
              FormSection(
                title: 'Trip reminders',
                children: [
                  NumberField(
                    controller: _delay,
                    label: 'First reminder after (minutes)',
                    min: 1,
                    max: 1440,
                    errorText: _error?.fieldError('reminderDelayMinutes'),
                    helperText: delay == null
                        ? null
                        : 'The driver is reminded ${formatDuration(Duration(minutes: delay))} after the start, then every '
                              '${settings.reminderRepeatMinutes} min. The trip shows as overdue from the first reminder.',
                  ),
                ],
              ),
            ],
          ),
        ),
      ],
    );
  }
}
