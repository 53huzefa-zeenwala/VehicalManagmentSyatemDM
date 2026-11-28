import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../state.dart';
import '../widgets/admin_widgets.dart';
import 'live.dart';

/// A-16 list: staff (drivers), search by name, Staff ID or mobile; active filter.
class StaffListScreen extends ConsumerStatefulWidget {
  const StaffListScreen({super.key});

  @override
  ConsumerState<StaffListScreen> createState() => _StaffListScreenState();
}

class _StaffListScreenState extends ConsumerState<StaffListScreen> {
  String _search = '';
  bool? _active = true;
  int _version = 0;

  Future<void> _open(String path) async {
    await context.push(path);
    if (mounted) {
      setState(() => _version++);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Staff')),
    floatingActionButton: FloatingActionButton.extended(
      onPressed: () => _open('/staff/new'),
      icon: const Icon(Icons.person_add_rounded),
      label: const Text('Add staff'),
    ),
    body: Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(DmSize.gutter, 4, DmSize.gutter, 8),
          child: Column(
            children: [
              SearchField(hint: 'Name, Staff ID or mobile', onChanged: (v) => setState(() => _search = v)),
              const SizedBox(height: 10),
              ChoiceChips<bool>(
                options: const {true: 'Active', false: 'Inactive', null: 'All'},
                selected: _active,
                onSelected: (v) => setState(() => _active = v),
              ),
            ],
          ),
        ),
        Expanded(
          child: PagedListView<StaffMember>(
            key: ValueKey('$_search|$_active|$_version'),
            padding: const EdgeInsets.fromLTRB(DmSize.gutter, 4, DmSize.gutter, 96),
            fetch: (page) => ref.read(adminReposProvider).staffList(search: _search, isActive: _active, page: page),
            empty: const EmptyView(
              icon: Icons.badge_rounded,
              title: 'No staff',
              message: 'Add your drivers. Each gets a Staff ID and a password to sign in to DM Staff.',
            ),
            itemBuilder: (context, s) => ListRow(
              leading: Avatar(name: s.fullName, photoUrl: s.photoUrl, size: 42),
              title: s.fullName,
              subtitle: '${s.loginId} · ${s.mobile}',
              trailing: s.isActive ? null : activeBadge(false),
              onTap: () => _open('/staff/${s.id}'),
            ),
          ),
        ),
      ],
    ),
  );
}

/// A-16 detail: photo, details, licence, trips and hours for a date range; edit, reset password, (de)activate.
class StaffDetailScreen extends ConsumerStatefulWidget {
  const StaffDetailScreen({super.key, required this.id});

  final int id;

  @override
  ConsumerState<StaffDetailScreen> createState() => _StaffDetailScreenState();
}

class _StaffDetailScreenState extends ConsumerState<StaffDetailScreen> {
  DateRange _range = DateRange.thisMonth();

  int get id => widget.id;

  void _reload() => ref.invalidate(staffMemberProvider(id));

  Future<void> _toggleActive(StaffMember s) async {
    final sure = await confirmAction(
      context,
      title: s.isActive ? 'Deactivate ${s.fullName}?' : 'Activate ${s.fullName}?',
      message: s.isActive
          ? 'They are signed out at once and can’t sign in until you activate them again. Their trips and hours stay.'
          : 'They can sign in to DM Staff again.',
      confirmLabel: s.isActive ? 'Deactivate' : 'Activate',
      danger: s.isActive,
    );
    if (!sure) {
      return;
    }
    try {
      await ref.read(adminReposProvider).setStaffActive(id, isActive: !s.isActive, rowVersion: s.rowVersion);
      _reload();
      if (mounted) {
        showMessage(context, s.isActive ? 'Deactivated.' : 'Activated.');
      }
    } on ApiException catch (error) {
      if (mounted) {
        showSaveError(context, error, onReload: _reload);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final staff = ref.watch(staffMemberProvider(id));
    return Scaffold(
      appBar: AppBar(
        title: const Text('Staff'),
        actions: [
          if (staff.hasValue)
            IconButton(
              tooltip: 'Edit',
              icon: const Icon(Icons.edit_rounded),
              onPressed: () async {
                await context.push('/staff/$id/edit');
                _reload();
              },
            ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          _reload();
          await ref.read(staffMemberProvider(id).future);
        },
        child: AsyncBody(
          value: staff,
          onRetry: _reload,
          data: (s) => ListView(
            padding: const EdgeInsets.fromLTRB(DmSize.gutter, 8, DmSize.gutter, 24),
            children: [
              Row(
                children: [
                  PhotoEditor(
                    photoUrl: s.photoUrl,
                    size: 96,
                    placeholder: Avatar(name: s.fullName, size: 60),
                    upload: (bytes) async {
                      await ref.read(adminReposProvider).uploadStaffPhoto(id, bytes);
                      _reload();
                    },
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(s.fullName, style: context.text.titleLarge),
                        const SizedBox(height: 2),
                        InkWell(
                          onTap: () => copyText(context, s.loginId, what: 'Staff ID copied'),
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Text(s.loginId, style: dmStyle(15, FontWeight.w700, color: context.dm.primary, tabular: true)),
                              const SizedBox(width: 4),
                              Icon(Icons.copy_rounded, size: 16, color: context.dm.primary),
                            ],
                          ),
                        ),
                        const SizedBox(height: 6),
                        activeBadge(s.isActive),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 14),
              AppCard(
                child: Column(
                  children: [
                    KeyValueRow('Mobile', s.mobile),
                    if (s.email != null) KeyValueRow('Email', s.email!),
                    KeyValueRow('Driving licence', s.licenceNumber),
                    if (s.createdAtUtc != null) KeyValueRow('Added', Gst.dateTime(s.createdAtUtc!)),
                  ],
                ),
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton.icon(
                    onPressed: () => showResetPassword(
                      context,
                      name: s.fullName,
                      reset: (password) async {
                        await ref.read(adminReposProvider).resetStaffPassword(id, password);
                        // The reset updates the account (new rowVersion): reload before any further edit.
                        _reload();
                      },
                    ),
                    icon: const Icon(Icons.key_rounded),
                    label: const Text('Reset password'),
                  ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: OutlinedButton.icon(
                    style: s.isActive ? OutlinedButton.styleFrom(foregroundColor: context.dm.dText) : null,
                    onPressed: () => _toggleActive(s),
                    icon: Icon(s.isActive ? Icons.block_rounded : Icons.check_circle_rounded),
                    label: Text(s.isActive ? 'Deactivate' : 'Activate'),
                  ),
                  ),
                ],
              ),
              SectionLabel('Trips and hours', trailing: DateRangeButton(range: _range, onChanged: (r) => setState(() => _range = r))),
              _StaffTrips(staffId: id, range: _range),
            ],
          ),
        ),
      ),
    );
  }
}

class _StaffTrips extends ConsumerWidget {
  const _StaffTrips({required this.staffId, required this.range});

  final int staffId;
  final DateRange range;

  @override
  Widget build(BuildContext context, WidgetRef ref) => KeyedFutureBuilder<StaffTrips>(
    requestKey: range,
    load: () => ref.read(adminReposProvider).staffTrips(staffId, fromGst: range.from, toGst: range.to),
    builder: (context, snapshot, retry) {
      if (snapshot.hasError) {
        return ErrorView(error: snapshot.error!, compact: true, onRetry: retry);
      }
      if (!snapshot.hasData) {
        return const SkeletonList(count: 2, height: 70);
      }
      final data = snapshot.data!;
      return Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          AppCard(
            child: Column(
              children: [
                KeyValueRow('Trips', '${data.trips.totalCount}'),
                KeyValueRow('Hours', formatDuration(Duration(seconds: data.totalSeconds))),
              ],
            ),
          ),
          const SizedBox(height: 8),
          if (data.trips.items.isEmpty)
            Text('No trips in these dates.', style: context.text.bodyMedium)
          else
            for (final trip in data.trips.items) ...[TripRow(trip: trip), const SizedBox(height: 8)],
          if (data.trips.hasMore)
            Text('Showing the latest ${data.trips.items.length}. See Reports → Trip log for all.', style: context.text.bodySmall),
        ],
      );
    },
  );
}

/// Sets a new password for someone else (staff or admin). Shows it once with Copy, so the admin can pass it on.
Future<void> showResetPassword(BuildContext context, {required String name, required Future<void> Function(String) reset}) async {
  final controller = TextEditingController();
  final form = GlobalKey<FormState>();
  final password = await showDialog<String>(
    context: context,
    builder: (context) => AlertDialog(
      title: Text('New password for $name'),
      content: Form(
        key: form,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextFormField(
              controller: controller,
              autofocus: true,
              decoration: const InputDecoration(labelText: 'New password', helperText: '8 to 128 characters'),
              validator: passwordValidator,
            ),
            const SizedBox(height: 8),
            Text('This also unlocks the account and signs them out everywhere.', style: context.text.bodySmall),
          ],
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.of(context).pop(), child: const Text('Cancel')),
        TextButton(
          onPressed: () {
            if (form.currentState!.validate()) {
              Navigator.of(context).pop(controller.text);
            }
          },
          child: const Text('Reset'),
        ),
      ],
    ),
  );
  controller.dispose();
  if (password == null || !context.mounted) {
    return;
  }
  try {
    await reset(password);
    if (context.mounted) {
      await showCredentials(context, title: 'Password reset', loginId: null, password: password);
    }
  } on ApiException catch (error) {
    if (context.mounted) {
      showError(context, error);
    }
  }
}

/// Shows sign-in details once (new account or reset), with Copy.
Future<void> showCredentials(BuildContext context, {required String title, required String? loginId, required String password}) =>
    showDialog<void>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(title),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Give these to the person. They can change the password after signing in.'),
            const SizedBox(height: 12),
            if (loginId != null) KeyValueRow('ID', loginId),
            KeyValueRow('Password', password),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => copyText(
              context,
              loginId == null ? password : 'ID: $loginId\nPassword: $password',
              what: 'Copied',
            ),
            child: const Text('Copy'),
          ),
          TextButton(onPressed: () => Navigator.of(context).pop(), child: const Text('Done')),
        ],
      ),
    );

/// A-16 form: name, mobile, email, licence; on create also the first password. The Staff ID is generated.
class StaffFormScreen extends ConsumerStatefulWidget {
  const StaffFormScreen({super.key, this.id});

  final int? id;

  @override
  ConsumerState<StaffFormScreen> createState() => _StaffFormScreenState();
}

class _StaffFormScreenState extends ConsumerState<StaffFormScreen> {
  final _form = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _mobile = TextEditingController();
  final _email = TextEditingController();
  final _licence = TextEditingController();
  final _password = TextEditingController();
  StaffMember? _existing;
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
      final s = await ref.read(adminReposProvider).staffMember(widget.id!);
      if (!mounted) {
        return;
      }
      setState(() {
        _existing = s;
        _name.text = s.fullName;
        _mobile.text = s.mobile;
        _email.text = s.email ?? '';
        _licence.text = s.licenceNumber;
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
    for (final c in [_name, _mobile, _email, _licence, _password]) {
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
        await repos.updateStaff(
          widget.id!,
          fullName: _name.text,
          mobile: _mobile.text,
          email: _email.text,
          licenceNumber: _licence.text,
          rowVersion: _existing!.rowVersion,
        );
        if (mounted) {
          showMessage(context, 'Saved.');
          context.pop(true);
        }
        return;
      }
      final created = await repos.createStaff(
        fullName: _name.text,
        mobile: _mobile.text,
        email: _email.text,
        licenceNumber: _licence.text,
        password: _password.text,
      );
      if (!mounted) {
        return;
      }
      await showCredentials(context, title: 'Staff added', loginId: created.loginId, password: _password.text);
      if (mounted) {
        context.pushReplacement('/staff/${created.id}');
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
        appBar: AppBar(title: const Text('Edit staff')),
        body: _error == null ? const SkeletonList() : ErrorView(error: _error!, onRetry: _load),
      );
    }
    return FlowScaffold(
      title: _isEdit ? 'Edit staff' : 'Add staff',
      bottom: BusyButton(label: _isEdit ? 'Save' : 'Add staff', onPressed: _save),
      children: [
        Form(
          key: _form,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              FormSection(
                title: 'Details',
                children: accountFields(
                  name: _name,
                  mobile: _mobile,
                  email: _email,
                  error: _error,
                  extra: [
                    TextFormField(
                      controller: _licence,
                      textCapitalization: TextCapitalization.characters,
                      decoration: InputDecoration(labelText: 'Driving licence number', errorText: _error?.fieldError('licenceNumber')),
                      validator: (v) => (v ?? '').trim().isEmpty ? 'Enter the driving licence number.' : null,
                    ),
                  ],
                ),
              ),
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
              if (!_isEdit)
                Padding(
                  padding: const EdgeInsets.only(top: 12),
                  child: Text('The Staff ID is made automatically (prefix + year + number).', style: context.text.bodySmall),
                ),
            ],
          ),
        ),
      ],
    );
  }
}

/// Name, mobile, optional email (shared by the staff and admin forms), plus [extra] fields.
List<Widget> accountFields({
  required TextEditingController name,
  required TextEditingController mobile,
  required TextEditingController email,
  required ApiException? error,
  List<Widget> extra = const [],
}) => [
  TextFormField(
    controller: name,
    textCapitalization: TextCapitalization.words,
    decoration: InputDecoration(labelText: 'Full name', errorText: error?.fieldError('fullName')),
    validator: (v) => (v ?? '').trim().isEmpty ? 'Enter the full name.' : null,
  ),
  TextFormField(
    controller: mobile,
    keyboardType: TextInputType.phone,
    decoration: InputDecoration(labelText: 'Mobile', hintText: '+9715…', errorText: error?.fieldError('mobile')),
    validator: (v) => (v ?? '').trim().isEmpty ? 'Enter a mobile number.' : null,
  ),
  TextFormField(
    controller: email,
    keyboardType: TextInputType.emailAddress,
    decoration: InputDecoration(labelText: 'Email (optional)', errorText: error?.fieldError('email')),
  ),
  ...extra,
];
