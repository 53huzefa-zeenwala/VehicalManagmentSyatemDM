import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_svg/flutter_svg.dart';

/// C-02: emerald header with the truck, then Staff ID and password. Errors come from the API (wrong ID or
/// password, locked, deactivated, connection).
class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _form = GlobalKey<FormState>();
  final _loginId = TextEditingController();
  final _password = TextEditingController();
  bool _hidePassword = true;
  bool _busy = false;
  String? _error;

  @override
  void dispose() {
    _loginId.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_form.currentState!.validate()) {
      return;
    }
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await ref.read(sessionProvider.notifier).login(_loginId.text, _password.text);
    } on ApiException catch (error) {
      if (mounted) {
        setState(() => _error = error.detail);
      }
    } finally {
      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final c = context.dm;
    final session = ref.watch(sessionProvider);
    final notice = _error ?? (session is SignedOut ? session.message : null);
    return Scaffold(
      backgroundColor: c.pDeep,
      body: SingleChildScrollView(
        child: Column(
          children: [
            Container(
              height: 330,
              decoration: BoxDecoration(gradient: c.hero),
              child: Stack(
                children: [
                  Positioned.fill(
                    child: Opacity(
                      opacity: .2,
                      child: SvgPicture.asset('packages/dm_core/assets/illustrations/routes.svg', fit: BoxFit.cover),
                    ),
                  ),
                  SafeArea(
                    child: Padding(
                      padding: const EdgeInsets.all(DmSize.gutter + 4),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Container(
                            width: 52,
                            height: 52,
                            decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(16)),
                            child: Icon(Icons.local_shipping_rounded, color: c.isDark ? c.pDeep : c.primary, size: 30),
                          ),
                          const SizedBox(height: 18),
                          Text('Fleet & Parking', style: dmStyle(30, FontWeight.w800, color: Colors.white, spacing: -.8)),
                          Text('Staff', style: dmStyle(15, FontWeight.w600, color: Colors.white70)),
                          const Spacer(),
                          const Align(
                            alignment: Alignment.bottomRight,
                            child: Illustration('truck', height: 96, color: Colors.white24),
                          ),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
            Container(
              decoration: BoxDecoration(
                color: c.surface,
                borderRadius: const BorderRadius.vertical(top: Radius.circular(32)),
              ),
              padding: const EdgeInsets.fromLTRB(DmSize.gutter + 4, 28, DmSize.gutter + 4, 32),
              constraints: BoxConstraints(minHeight: MediaQuery.sizeOf(context).height - 330),
              child: Form(
                key: _form,
                child: AutofillGroup(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Text('Sign in', style: context.text.headlineMedium),
                      const SizedBox(height: 4),
                      Text('Use the Staff ID and password from your admin.', style: context.text.bodyMedium),
                      const SizedBox(height: 22),
                      if (notice != null) ...[
                        Container(
                          padding: const EdgeInsets.all(12),
                          decoration: BoxDecoration(color: c.dSoft, borderRadius: BorderRadius.circular(14)),
                          child: Row(
                            children: [
                              Icon(Icons.error_rounded, color: c.dText, size: 20),
                              const SizedBox(width: 8),
                              Expanded(child: Text(notice, style: dmStyle(13.5, FontWeight.w600, color: c.dText))),
                            ],
                          ),
                        ),
                        const SizedBox(height: 16),
                      ],
                      TextFormField(
                        key: const Key('loginId'),
                        controller: _loginId,
                        textCapitalization: TextCapitalization.characters,
                        autofillHints: const [AutofillHints.username],
                        textInputAction: TextInputAction.next,
                        decoration: const InputDecoration(labelText: 'Staff ID', prefixIcon: Icon(Icons.badge_rounded)),
                        validator: (value) => (value ?? '').trim().isEmpty ? 'Enter your Staff ID.' : null,
                      ),
                      const SizedBox(height: 14),
                      TextFormField(
                        key: const Key('password'),
                        controller: _password,
                        obscureText: _hidePassword,
                        autofillHints: const [AutofillHints.password],
                        textInputAction: TextInputAction.done,
                        onFieldSubmitted: (_) => _submit(),
                        decoration: InputDecoration(
                          labelText: 'Password',
                          prefixIcon: const Icon(Icons.lock_rounded),
                          suffixIcon: IconButton(
                            tooltip: _hidePassword ? 'Show password' : 'Hide password',
                            icon: Icon(_hidePassword ? Icons.visibility_rounded : Icons.visibility_off_rounded),
                            onPressed: () => setState(() => _hidePassword = !_hidePassword),
                          ),
                        ),
                        validator: (value) => (value ?? '').isEmpty ? 'Enter your password.' : null,
                      ),
                      Align(
                        alignment: Alignment.centerRight,
                        child: TextButton(onPressed: () => _forgotPassword(context), child: const Text('Forgot password?')),
                      ),
                      const SizedBox(height: 8),
                      FilledButton(
                        key: const Key('signIn'),
                        onPressed: _busy ? null : _submit,
                        child: _busy
                            ? const SizedBox(width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2.4))
                            : const Text('Sign in'),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  void _forgotPassword(BuildContext context) => showModalBottomSheet<void>(
    context: context,
    builder: (_) => Padding(
      padding: const EdgeInsets.fromLTRB(DmSize.gutter, 0, DmSize.gutter, 32),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Forgot your password?', style: context.text.titleLarge),
          const SizedBox(height: 8),
          Text(
            'Ask your admin to reset it. They give you a new password, and you can change it in Profile after signing in.',
            style: context.text.bodyMedium,
          ),
          const SizedBox(height: 18),
          FilledButton(onPressed: () => Navigator.of(context).pop(), child: const Text('OK')),
        ],
      ),
    ),
  );
}
