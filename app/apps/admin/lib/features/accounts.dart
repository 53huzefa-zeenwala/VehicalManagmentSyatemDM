import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../state.dart';
import '../widgets/admin_widgets.dart';
import 'staff.dart';

/// A-17: admin accounts. Add, edit, reset password, activate or deactivate (never yourself, AUTH-06).
class AdminsScreen extends ConsumerWidget {
  const AdminsScreen({super.key});

  Future<void> _toggle(BuildContext context, WidgetRef ref, AdminAccount a) async {
    final sure = await confirmAction(
      context,
      title: a.isActive ? 'Deactivate ${a.fullName}?' : 'Activate ${a.fullName}?',
      message: a.isActive ? 'They are signed out at once and can’t sign in.' : 'They can sign in to DM Admin again.',
      confirmLabel: a.isActive ? 'Deactivate' : 'Activate',
      danger: a.isActive,
    );
    if (!sure) {
      return;
    }
    try {
      await ref.read(adminReposProvider).setAdminActive(a.id, isActive: !a.isActive, rowVersion: a.rowVersion);
      ref.invalidate(adminsProvider);
    } on ApiException catch (error) {
      if (context.mounted) {
        showSaveError(context, error, onReload: () => ref.invalidate(adminsProvider));
      }
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final admins = ref.watch(adminsProvider);
    final me = ref.watch(meProvider);
    return Scaffold(
      appBar: AppBar(title: const Text('Admins')),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () async {
          await context.push('/admins/new');
          ref.invalidate(adminsProvider);
        },
        icon: const Icon(Icons.person_add_rounded),
        label: const Text('Add admin'),
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(adminsProvider);
          await ref.read(adminsProvider.future);
        },
        child: AsyncBody(
          value: admins,
          onRetry: () => ref.invalidate(adminsProvider),
          data: (items) => ListView.separated(
            padding: const EdgeInsets.fromLTRB(DmSize.gutter, 4, DmSize.gutter, 96),
            itemCount: items.length,
            separatorBuilder: (_, _) => const SizedBox(height: 8),
            itemBuilder: (context, i) {
              final a = items[i];
              final isMe = a.id == me.id;
              return AppCard(
                padding: const EdgeInsets.fromLTRB(14, 10, 4, 10),
                child: Row(
                  children: [
                    Avatar(name: a.fullName, size: 42),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(isMe ? '${a.fullName} (you)' : a.fullName, style: context.text.titleSmall),
                          Text('${a.loginId} · ${a.mobile}', style: context.text.bodySmall),
                        ],
                      ),
                    ),
                    if (!a.isActive) activeBadge(false),
                    PopupMenuButton<int>(
                      tooltip: 'More',
                      onSelected: (value) async {
                        switch (value) {
                          case 0:
                            await context.push('/admins/${a.id}/edit');
                            ref.invalidate(adminsProvider);
                          case 1:
                            await showResetPassword(
                              context,
                              name: a.fullName,
                              reset: (password) => ref.read(adminReposProvider).resetAdminPassword(a.id, password),
                            );
                          case 2:
                            await _toggle(context, ref, a);
                        }
                      },
                      itemBuilder: (_) => [
                        const PopupMenuItem(value: 0, child: Text('Edit')),
                        if (!isMe) const PopupMenuItem(value: 1, child: Text('Reset password')),
                        if (!isMe) PopupMenuItem(value: 2, child: Text(a.isActive ? 'Deactivate' : 'Activate')),
                      ],
                    ),
                  ],
                ),
              );
            },
          ),
        ),
      ),
    );
  }
}

/// A-17 form: name, mobile, email; on create also the first password. The Admin ID (ADM + YY + 5 digits) is made
/// by the server (D-037).
class AdminFormScreen extends ConsumerStatefulWidget {
  const AdminFormScreen({super.key, this.id});

  final int? id;

  @override
  ConsumerState<AdminFormScreen> createState() => _AdminFormScreenState();
}

class _AdminFormScreenState extends ConsumerState<AdminFormScreen> {
  final _form = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _mobile = TextEditingController();
  final _email = TextEditingController();
  final _password = TextEditingController();
  AdminAccount? _existing;
  ApiException? _error;
  bool _loaded = false;

  bool get _isEdit => widget.id != null;

  @override
  void initState() {
    super.initState();
    if (_isEdit) {
      _load();
    } else {
      _loaded = true;
    }
  }

  Future<void> _load() async {
    try {
      final a = await ref.read(adminReposProvider).admin(widget.id!);
      if (!mounted) {
        return;
      }
      setState(() {
        _existing = a;
        _name.text = a.fullName;
        _mobile.text = a.mobile;
        _email.text = a.email ?? '';
        _loaded = true;
      });
    } on ApiException catch (error) {
      if (mounted) {
        setState(() => _error = error);
      }
    }
  }

  @override
  void dispose() {
    for (final c in [_name, _mobile, _email, _password]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _save() async {
    setState(() => _error = null);
    if (!_form.currentState!.validate()) {
      return;
    }
    final repos = ref.read(adminReposProvider);
    try {
      if (_isEdit) {
        await repos.updateAdmin(
          widget.id!,
          fullName: _name.text,
          mobile: _mobile.text,
          email: _email.text,
          rowVersion: _existing!.rowVersion,
        );
        if (widget.id == ref.read(meProvider).id) {
          await ref.read(sessionProvider.notifier).refreshMe();
        }
        if (mounted) {
          showMessage(context, 'Saved.');
          context.pop(true);
        }
        return;
      }
      final created = await repos.createAdmin(
        fullName: _name.text,
        mobile: _mobile.text,
        email: _email.text,
        password: _password.text,
      );
      if (mounted) {
        await showCredentials(context, title: 'Admin added', loginId: created.loginId, password: _password.text);
      }
      if (mounted) {
        context.pop(true);
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
    if (!_loaded) {
      return Scaffold(
        appBar: AppBar(title: const Text('Edit admin')),
        body: _error == null ? const SkeletonList() : ErrorView(error: _error!, onRetry: _load),
      );
    }
    return FlowScaffold(
      title: _isEdit ? 'Edit admin' : 'Add admin',
      bottom: BusyButton(label: _isEdit ? 'Save' : 'Add admin', onPressed: _save),
      children: [
        Form(
          key: _form,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              FormSection(title: 'Details', children: accountFields(name: _name, mobile: _mobile, email: _email, error: _error)),
              if (!_isEdit)
                FormSection(
                  title: 'First password',
                  children: [
                    TextFormField(
                      controller: _password,
                      decoration: InputDecoration(
                        labelText: 'Password',
                        helperText: '8 to 128 characters. Shown once after saving.',
                        errorText: _error?.fieldError('password'),
                      ),
                      validator: passwordValidator,
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
