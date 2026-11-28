import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../state.dart';
import '../widgets/admin_widgets.dart';

/// Filters for the vehicle list (A-09).
class VehicleFilter {
  const VehicleFilter({this.search = '', this.status, this.typeId, this.lotId, this.notAtLot = false});

  final String search;
  final VehicleStatus? status;
  final int? typeId;
  final int? lotId;
  final bool notAtLot;

  VehicleFilter copyWith({String? search, Object? status = _keep, Object? typeId = _keep, Object? lotId = _keep, bool? notAtLot}) =>
      VehicleFilter(
        search: search ?? this.search,
        status: status == _keep ? this.status : status as VehicleStatus?,
        typeId: typeId == _keep ? this.typeId : typeId as int?,
        lotId: lotId == _keep ? this.lotId : lotId as int?,
        notAtLot: notAtLot ?? this.notAtLot,
      );

  static const _keep = Object();

  /// Stable key so the paged list restarts when a filter changes.
  String get key => '$search|$status|$typeId|$lotId|$notAtLot';
}

/// A-09: vehicles with search and filters (status, type, lot, "Not at a lot"), paged.
class VehiclesScreen extends ConsumerStatefulWidget {
  const VehiclesScreen({super.key, this.notAtLot = false});

  final bool notAtLot;

  @override
  ConsumerState<VehiclesScreen> createState() => _VehiclesScreenState();
}

class _VehiclesScreenState extends ConsumerState<VehiclesScreen> {
  late VehicleFilter _filter = VehicleFilter(notAtLot: widget.notAtLot);
  int _version = 0;

  Future<void> _open(String path) async {
    await context.push(path);
    if (mounted) {
      setState(() => _version++);
    }
  }

  @override
  Widget build(BuildContext context) {
    final types = ref.watch(activeTypeOptionsProvider).valueOrNull ?? const [];
    final lots = ref.watch(allLotsProvider).valueOrNull ?? const [];
    return Scaffold(
      appBar: AppBar(title: const Text('Vehicles')),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => _open('/vehicles/new'),
        icon: const Icon(Icons.add_rounded),
        label: const Text('Add vehicle'),
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(DmSize.gutter, 4, DmSize.gutter, 8),
            child: Column(
              children: [
                SearchField(
                  hint: 'Registration, brand or model',
                  onChanged: (v) => setState(() => _filter = _filter.copyWith(search: v)),
                ),
                const SizedBox(height: 10),
                ChoiceChips<String>(
                  options: const {null: 'All', 'available': 'Available', 'active': 'On the road', 'maintenance': 'Maintenance', 'inactive': 'Inactive', 'notAtLot': 'Not at a lot'},
                  selected: _filter.notAtLot
                      ? 'notAtLot'
                      : switch (_filter.status) {
                          VehicleStatus.available => 'available',
                          VehicleStatus.active => 'active',
                          VehicleStatus.underMaintenance => 'maintenance',
                          VehicleStatus.inactive => 'inactive',
                          null => null,
                        },
                  onSelected: (value) => setState(
                    () => _filter = _filter.copyWith(
                      notAtLot: value == 'notAtLot',
                      status: switch (value) {
                        'available' => VehicleStatus.available,
                        'active' => VehicleStatus.active,
                        'maintenance' => VehicleStatus.underMaintenance,
                        'inactive' => VehicleStatus.inactive,
                        _ => null,
                      },
                    ),
                  ),
                ),
                const SizedBox(height: 10),
                Row(
                  children: [
                    Expanded(
                      child: DropdownButtonFormField<int?>(
                        initialValue: _filter.typeId,
                        isExpanded: true,
                        decoration: const InputDecoration(labelText: 'Type'),
                        items: [
                          const DropdownMenuItem(value: null, child: Text('All types')),
                          for (final t in types) DropdownMenuItem(value: t.id, child: Text(t.label)),
                        ],
                        onChanged: (v) => setState(() => _filter = _filter.copyWith(typeId: v)),
                      ),
                    ),
                    const SizedBox(width: 10),
                    Expanded(
                      child: DropdownButtonFormField<int?>(
                        initialValue: _filter.lotId,
                        isExpanded: true,
                        decoration: const InputDecoration(labelText: 'Lot'),
                        items: [
                          const DropdownMenuItem(value: null, child: Text('All lots')),
                          for (final l in lots) DropdownMenuItem(value: l.id, child: Text(l.name)),
                        ],
                        onChanged: (v) => setState(() => _filter = _filter.copyWith(lotId: v)),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
          Expanded(
            child: PagedListView<VehicleDetail>(
              key: ValueKey('${_filter.key}#$_version'),
              padding: const EdgeInsets.fromLTRB(DmSize.gutter, 4, DmSize.gutter, 96),
              fetch: (page) => ref
                  .read(adminReposProvider)
                  .vehicles(
                    search: _filter.search,
                    status: _filter.status,
                    typeId: _filter.typeId,
                    lotId: _filter.lotId,
                    notAtLot: _filter.notAtLot,
                    page: page,
                  ),
              empty: const EmptyView(
                icon: Icons.directions_car_rounded,
                title: 'No vehicles',
                message: 'Add a vehicle, or change the filters. Each vehicle gets its own QR code.',
              ),
              itemBuilder: (context, v) => VehicleRow(vehicle: v, onTap: () => _open('/vehicles/${v.id}')),
            ),
          ),
        ],
      ),
    );
  }
}

class VehicleRow extends StatelessWidget {
  const VehicleRow({super.key, required this.vehicle, this.onTap, this.trailing});

  final VehicleDetail vehicle;
  final VoidCallback? onTap;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    final v = vehicle;
    final place = v.isNotAtLot ? 'Not at a lot' : (v.currentLot?.name ?? 'Out');
    return AppCard(
      onTap: onTap,
      padding: const EdgeInsets.all(12),
      child: Row(
        children: [
          ClipRRect(
            borderRadius: BorderRadius.circular(DmSize.rTile),
            child: SizedBox(
              width: 56,
              height: 56,
              child: v.photoUrl == null
                  ? ColoredBox(
                      color: context.dm.surface2,
                      child: Icon(Icons.directions_car_rounded, color: context.dm.text3),
                    )
                  : AuthImage(v.photoUrl!, width: 56, height: 56),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    PlateBadge(v.registrationNumber),
                    const Spacer(),
                    if (trailing == null)
                      v.isNotAtLot
                          ? const StatusBadge('Not at a lot', tone: Tone.warn, icon: Icons.wrong_location_rounded)
                          : vehicleStatusBadge(v.status),
                  ],
                ),
                const SizedBox(height: 4),
                Text('${v.brand} ${v.model} · ${v.colour}', style: context.text.titleSmall, overflow: TextOverflow.ellipsis),
                Text('${v.category} · ${v.type} · $place', style: context.text.bodySmall, overflow: TextOverflow.ellipsis),
              ],
            ),
          ),
          ?trailing,
        ],
      ),
    );
  }
}

/// A-10: details, status, location, photo, QR label and history. Edits use the record's rowVersion.
class VehicleDetailScreen extends ConsumerWidget {
  const VehicleDetailScreen({super.key, required this.id});

  final int id;

  void _reload(WidgetRef ref) => ref
    ..invalidate(vehicleProvider(id))
    ..invalidate(dashboardProvider)
    ..invalidate(allLotsProvider);

  Future<void> _setStatus(BuildContext context, WidgetRef ref, VehicleDetail v) async {
    final status = await showModalBottomSheet<VehicleStatus>(
      context: context,
      builder: (context) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const ListTile(title: Text('Set status')),
            for (final s in [VehicleStatus.available, VehicleStatus.underMaintenance, VehicleStatus.inactive])
              ListTile(
                leading: Icon(s == v.status ? Icons.radio_button_checked_rounded : Icons.radio_button_unchecked_rounded),
                title: Text(vehicleStatusLabel(s)),
                subtitle: Text(switch (s) {
                  VehicleStatus.available => 'Staff can start trips with it',
                  VehicleStatus.underMaintenance => 'Blocked for trips until it is available again',
                  _ => 'Out of service; hidden from staff',
                }),
                onTap: () => Navigator.of(context).pop(s),
              ),
          ],
        ),
      ),
    );
    if (status == null || status == v.status || !context.mounted) {
      return;
    }
    try {
      await ref.read(adminReposProvider).setVehicleStatus(id, status, v.rowVersion);
      _reload(ref);
      if (context.mounted) {
        showMessage(context, 'Status set to ${vehicleStatusLabel(status).toLowerCase()}.');
      }
    } on ApiException catch (error) {
      if (context.mounted) {
        showSaveError(context, error, onReload: () => _reload(ref));
      }
    }
  }

  Future<void> _place(BuildContext context, WidgetRef ref, VehicleDetail v) async {
    final lots = await ref.read(activeLotsProvider.future);
    if (!context.mounted) {
      return;
    }
    final lotId = await showModalBottomSheet<int>(
      context: context,
      isScrollControlled: true,
      builder: (context) => SafeArea(
        child: ListView(
          shrinkWrap: true,
          children: [
            const ListTile(title: Text('Where is it parked?')),
            for (final lot in lots)
              ListTile(
                leading: const Icon(Icons.local_parking_rounded),
                title: Text(lot.name),
                subtitle: Text('${lot.total.available} free of ${lot.total.capacity}'),
                onTap: () => Navigator.of(context).pop(lot.id),
              ),
          ],
        ),
      ),
    );
    if (lotId == null || !context.mounted) {
      return;
    }
    try {
      await ref.read(adminReposProvider).setVehicleLocation(id, lotId, v.rowVersion);
      _reload(ref);
      if (context.mounted) {
        showMessage(context, 'Location saved.');
      }
    } on ApiException catch (error) {
      if (context.mounted) {
        showSaveError(context, error, onReload: () => _reload(ref));
      }
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final vehicle = ref.watch(vehicleProvider(id));
    return Scaffold(
      appBar: AppBar(
        title: const Text('Vehicle'),
        actions: [
          if (vehicle.hasValue)
            IconButton(
              tooltip: 'Edit',
              icon: const Icon(Icons.edit_rounded),
              onPressed: () async {
                await context.push('/vehicles/$id/edit');
                _reload(ref);
              },
            ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          _reload(ref);
          await ref.read(vehicleProvider(id).future);
        },
        child: AsyncBody(
          value: vehicle,
          onRetry: () => _reload(ref),
          data: (v) {
            final out = v.status == VehicleStatus.active;
            return ListView(
              padding: const EdgeInsets.fromLTRB(DmSize.gutter, 8, DmSize.gutter, 24),
              children: [
                Center(
                  child: PhotoEditor(
                    photoUrl: v.photoUrl,
                    size: 150,
                    placeholder: Illustration(v.category.toLowerCase().contains('car') ? 'car' : 'truck', height: 70),
                    upload: (bytes) async {
                      await ref.read(adminReposProvider).uploadVehiclePhoto(id, bytes);
                      _reload(ref);
                    },
                  ),
                ),
                const SizedBox(height: 14),
                AppCard(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          PlateBadge(v.registrationNumber, large: true),
                          const Spacer(),
                          if (v.isNotAtLot)
                            const StatusBadge('Not at a lot', tone: Tone.warn, icon: Icons.wrong_location_rounded)
                          else
                            vehicleStatusBadge(v.status),
                        ],
                      ),
                      const SizedBox(height: 10),
                      KeyValueRow('Type', '${v.category} · ${v.type}'),
                      KeyValueRow('Brand · model', '${v.brand} ${v.model}'),
                      KeyValueRow('Colour', v.colour),
                      KeyValueRow('Parked at', v.isNotAtLot ? 'Not at a lot' : (v.currentLot?.name ?? 'Out on a trip or transfer')),
                      if (v.createdAtUtc != null) KeyValueRow('Added', Gst.dateTime(v.createdAtUtc!)),
                    ],
                  ),
                ),
                if (v.isNotAtLot) ...[
                  const SizedBox(height: 12),
                  Container(
                    padding: const EdgeInsets.all(14),
                    decoration: BoxDecoration(color: context.dm.wSoft, borderRadius: BorderRadius.circular(16)),
                    child: Text(
                      'This vehicle isn’t at a lot. Find it, then set the lot it is parked at.',
                      style: dmStyle(13.5, FontWeight.w600, color: context.dm.warn),
                    ),
                  ),
                ],
                const SizedBox(height: 14),
                GridView.count(
                  crossAxisCount: 2,
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  mainAxisSpacing: 8,
                  crossAxisSpacing: 8,
                  childAspectRatio: 3.2,
                  children: [
                    OutlinedButton.icon(
                      onPressed: out ? null : () => _setStatus(context, ref, v),
                      icon: const Icon(Icons.toggle_on_rounded),
                      label: const Text('Status'),
                    ),
                    OutlinedButton.icon(
                      onPressed: out ? null : () => _place(context, ref, v),
                      icon: const Icon(Icons.local_parking_rounded),
                      label: Text(v.isNotAtLot ? 'Set lot' : 'Move to lot'),
                    ),
                    OutlinedButton.icon(
                      onPressed: () => context.push('/vehicles/$id/label'),
                      icon: const Icon(Icons.qr_code_2_rounded),
                      label: const Text('QR label'),
                    ),
                    OutlinedButton.icon(
                      onPressed: v.status == VehicleStatus.available && v.currentLot != null
                          ? () => context.push('/transfers/new', extra: v)
                          : null,
                      icon: const Icon(Icons.local_shipping_rounded),
                      label: const Text('Transfer'),
                    ),
                  ],
                ),
                if (out)
                  Padding(
                    padding: const EdgeInsets.only(top: 8),
                    child: Text('Status and location can change once the vehicle is back.', style: context.text.bodySmall),
                  ),
                const SectionLabel('History'),
                // Keyed by rowVersion: reloads after every change to the vehicle (status, place, details, photo).
                _History(key: ValueKey(v.rowVersion), vehicleId: id),
              ],
            );
          },
        ),
      ),
    );
  }
}

class _History extends ConsumerStatefulWidget {
  const _History({super.key, required this.vehicleId});

  final int vehicleId;

  @override
  ConsumerState<_History> createState() => _HistoryState();
}

class _HistoryState extends ConsumerState<_History> {
  late Future<PagedResult<VehicleHistoryEntry>> _future = _load();

  Future<PagedResult<VehicleHistoryEntry>> _load() => ref.read(adminReposProvider).vehicleHistory(widget.vehicleId);

  @override
  Widget build(BuildContext context) => FutureBuilder(
    future: _future,
    builder: (context, snapshot) {
      if (snapshot.hasError) {
        return ErrorView(error: snapshot.error!, compact: true, onRetry: () => setState(() => _future = _load()));
      }
      if (!snapshot.hasData) {
        return const SkeletonList(count: 2, height: 56);
      }
      final items = snapshot.data!.items;
      if (items.isEmpty) {
        return Text('Nothing yet.', style: context.text.bodyMedium);
      }
      return AppCard(
        child: Column(
          children: [
            for (final entry in items)
              InkWell(
                onTap: switch ((entry.tripId, entry.transferId)) {
                  (final int tripId, _) => () => context.push('/trips/$tripId'),
                  (_, final int transferId) => () => context.push('/transfers/$transferId'),
                  _ => null,
                },
                child: Padding(
                  padding: const EdgeInsets.symmetric(vertical: 8),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Icon(
                        switch (entry.kind) {
                          'Trip' => Icons.route_rounded,
                          'Transfer' => Icons.local_shipping_rounded,
                          _ => Icons.edit_note_rounded,
                        },
                        size: 20,
                        color: context.dm.text3,
                      ),
                      const SizedBox(width: 10),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(entry.description, style: context.text.bodyMedium),
                            Text(
                              [
                                Gst.dateTime(entry.atUtc),
                                if (entry.durationSeconds != null) formatDuration(Duration(seconds: entry.durationSeconds!)),
                              ].join(' · '),
                              style: context.text.labelSmall,
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ),
          ],
        ),
      );
    },
  );
}

/// A-11: add or edit a vehicle. On create the lot (or "Not at a lot") and status are set; the server makes the
/// QR token.
class VehicleFormScreen extends ConsumerStatefulWidget {
  const VehicleFormScreen({super.key, this.id});

  final int? id;

  @override
  ConsumerState<VehicleFormScreen> createState() => _VehicleFormScreenState();
}

class _VehicleFormScreenState extends ConsumerState<VehicleFormScreen> {
  final _form = GlobalKey<FormState>();
  final _registration = TextEditingController();
  final _brand = TextEditingController();
  final _model = TextEditingController();
  final _colour = TextEditingController();
  int? _typeId;
  int? _lotId;
  bool _notAtLot = false;
  VehicleStatus _status = VehicleStatus.available;
  VehicleDetail? _existing;
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
      final v = await ref.read(adminReposProvider).vehicle(widget.id!);
      if (!mounted) {
        return;
      }
      setState(() {
        _existing = v;
        _registration.text = v.registrationNumber;
        _brand.text = v.brand;
        _model.text = v.model;
        _colour.text = v.colour;
        _typeId = v.vehicleTypeId;
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
    _registration.dispose();
    _brand.dispose();
    _model.dispose();
    _colour.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    setState(() => _error = null);
    if (!_form.currentState!.validate()) {
      return;
    }
    final repos = ref.read(adminReposProvider);
    try {
      final saved = _isEdit
          ? await repos.updateVehicle(
              widget.id!,
              registrationNumber: _registration.text,
              vehicleTypeId: _typeId!,
              brand: _brand.text,
              model: _model.text,
              colour: _colour.text,
              rowVersion: _existing!.rowVersion,
            )
          : await repos.createVehicle(
              registrationNumber: _registration.text,
              vehicleTypeId: _typeId!,
              brand: _brand.text,
              model: _model.text,
              colour: _colour.text,
              lotId: _notAtLot ? null : _lotId,
              status: _status,
            );
      ref
        ..invalidate(vehicleProvider(saved.id))
        ..invalidate(allLotsProvider)
        ..invalidate(dashboardProvider);
      if (!mounted) {
        return;
      }
      showMessage(context, _isEdit ? 'Vehicle saved.' : 'Vehicle added. Its QR label is ready.');
      if (_isEdit) {
        context.pop(true);
      } else {
        context.pushReplacement('/vehicles/${saved.id}');
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
    final types = ref.watch(activeTypeOptionsProvider);
    final lots = ref.watch(activeLotsProvider);
    if (!_loaded) {
      return Scaffold(
        appBar: AppBar(title: const Text('Edit vehicle')),
        body: _error == null ? const SkeletonList() : ErrorView(error: _error!, onRetry: _load),
      );
    }
    return FlowScaffold(
      title: _isEdit ? 'Edit vehicle' : 'Add vehicle',
      bottom: BusyButton(label: _isEdit ? 'Save' : 'Add vehicle', onPressed: _save),
      children: [
        Form(
          key: _form,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              FormSection(
                title: 'Vehicle',
                children: [
                  TextFormField(
                    controller: _registration,
                    textCapitalization: TextCapitalization.characters,
                    decoration: InputDecoration(
                      labelText: 'Registration number',
                      errorText: _error?.fieldError('registrationNumber'),
                    ),
                    validator: (v) => (v ?? '').trim().isEmpty ? 'Enter the registration number.' : null,
                  ),
                  AsyncBody(
                    value: types,
                    onRetry: () => ref.invalidate(categoriesProvider),
                    loading: const LinearProgressIndicator(),
                    data: (options) => DropdownButtonFormField<int>(
                      initialValue: options.any((o) => o.id == _typeId) ? _typeId : null,
                      isExpanded: true,
                      decoration: InputDecoration(
                        labelText: 'Category · type',
                        errorText: _error?.fieldError('vehicleTypeId'),
                        helperText: options.isEmpty ? 'Add a category and type first (Manage → Categories & types).' : null,
                      ),
                      items: [for (final o in options) DropdownMenuItem(value: o.id, child: Text(o.label))],
                      onChanged: (v) => setState(() => _typeId = v),
                      validator: (v) => v == null ? 'Pick a vehicle type.' : null,
                    ),
                  ),
                  TextFormField(
                    controller: _brand,
                    textCapitalization: TextCapitalization.words,
                    decoration: InputDecoration(labelText: 'Brand', errorText: _error?.fieldError('brand')),
                    validator: (v) => (v ?? '').trim().isEmpty ? 'Enter the brand.' : null,
                  ),
                  TextFormField(
                    controller: _model,
                    textCapitalization: TextCapitalization.words,
                    decoration: InputDecoration(labelText: 'Model', errorText: _error?.fieldError('model')),
                    validator: (v) => (v ?? '').trim().isEmpty ? 'Enter the model.' : null,
                  ),
                  TextFormField(
                    controller: _colour,
                    textCapitalization: TextCapitalization.words,
                    decoration: InputDecoration(labelText: 'Colour', errorText: _error?.fieldError('colour')),
                    validator: (v) => (v ?? '').trim().isEmpty ? 'Enter the colour.' : null,
                  ),
                ],
              ),
              if (!_isEdit)
                FormSection(
                  title: 'Where and status',
                  children: [
                    AsyncBody(
                      value: lots,
                      onRetry: () => ref.invalidate(allLotsProvider),
                      loading: const LinearProgressIndicator(),
                      data: (items) => DropdownButtonFormField<int>(
                        initialValue: _notAtLot ? -1 : _lotId,
                        isExpanded: true,
                        decoration: InputDecoration(labelText: 'Parked at', errorText: _error?.fieldError('lotId')),
                        items: [
                          for (final lot in items) DropdownMenuItem(value: lot.id, child: Text(lot.name)),
                          const DropdownMenuItem(value: -1, child: Text('Not at a lot')),
                        ],
                        onChanged: (v) => setState(() {
                          _notAtLot = v == -1;
                          _lotId = v == -1 ? null : v;
                        }),
                        validator: (v) => v == null ? 'Pick the lot (or Not at a lot).' : null,
                      ),
                    ),
                    DropdownButtonFormField<VehicleStatus>(
                      initialValue: _status,
                      decoration: const InputDecoration(labelText: 'Status'),
                      items: [
                        for (final s in [VehicleStatus.available, VehicleStatus.underMaintenance, VehicleStatus.inactive])
                          DropdownMenuItem(value: s, child: Text(vehicleStatusLabel(s))),
                      ],
                      onChanged: (v) => setState(() => _status = v ?? VehicleStatus.available),
                    ),
                  ],
                ),
              if (!_isEdit)
                Padding(
                  padding: const EdgeInsets.only(top: 12),
                  child: Text('Add a photo after saving, on the vehicle screen.', style: context.text.bodySmall),
                ),
            ],
          ),
        ),
      ],
    );
  }
}
