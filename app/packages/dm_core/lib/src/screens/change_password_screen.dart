import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../net/api_exception.dart';
import '../repos/repos.dart';
import '../widgets/flow.dart';
import '../widgets/widgets.dart';

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
        Navigator.of(context).pop();
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
              validator: passwordValidator,
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

/// Password rule shared by every password field (D-044): 8 to 128 characters.
String? passwordValidator(String? value) {
  final length = (value ?? '').length;
  return length < 8 || length > 128 ? 'Use 8 to 128 characters.' : null;
}
