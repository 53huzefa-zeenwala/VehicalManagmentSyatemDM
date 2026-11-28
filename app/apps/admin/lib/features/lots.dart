import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../state.dart';
import '../widgets/admin_widgets.dart';

String lotTypeLabel(LotType type) => switch (type) {
  LotType.open => 'Open',
  LotType.underground => 'Underground',
  LotType.other => 'Other',
};

/// A-18 list: every lot with live occupancy; inactive lots last.
class LotsScreen extends ConsumerWidget {
  const LotsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final lots = ref.watch(allLotsProvider);
    return Scaffold(
      appBar: AppBar(title: const Text('Parking lots')),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () async {
          await context.push('/lots/new');
          ref.invalidate(allLotsProvider);
        },
        icon: const Icon(Icons.add_rounded),
        label: const Text('Add lot'),
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(allLotsProvider);
          await ref.read(allLotsProvider.future);
        },
        child: AsyncBody(
          value: lots,
          onRetry: () => ref.invalidate(allLotsProvider),
          data: (items) {
            if (items.isEmpty) {
              return ListView(
                children: const [
                  EmptyView(
                    icon: Icons.local_parking_rounded,
                    title: 'No lots yet',
                    message: 'Add your parking lots with their capacity per vehicle type.',
                  ),
                ],
              );
            }
            final sorted = [...items]..sort((a, b) => a.isActive == b.isActive ? a.name.compareTo(b.name) : (a.isActive ? -1 : 1));
            return ListView.separated(
              padding: const EdgeInsets.fromLTRB(DmSize.gutter, 4, DmSize.gutter, 96),
              itemCount: sorted.length,
              separatorBuilder: (_, _) => const SizedBox(height: 10),
              itemBuilder: (context, i) => LotCard(lot: sorted[i], onTap: () => context.push('/lots/${sorted[i].id}')),
            );
          },
        ),
      ),
    );
  }
}

class LotCard extends StatelessWidget {
  const LotCard({super.key, required this.lot, this.onTap});

  final Lot lot;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final c = context.dm;
    final total = lot.total;
    final freeColor = total.isFull ? c.danger : (total.fill >= .85 ? c.warn : c.primary);
    return AppCard(
      onTap: onTap,
      flat: !lot.isActive,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('${lot.name} · ${lot.code}', style: context.text.titleMedium),
                    Text('${lot.hours} · ${lotTypeLabel(lot.lotType)}', style: context.text.bodySmall),
                  ],
                ),
              ),
              if (!lot.isActive)
                activeBadge(false)
              else
                Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Text('${total.available}', style: dmStyle(26, FontWeight.w800, color: freeColor, tabular: true)),
                    Text('free of ${total.capacity}', style: context.text.labelSmall),
                  ],
                ),
            ],
          ),
          const SizedBox(height: 10),
          CapacityBar(fill: total.fill, isFull: total.isFull),
          if (total.isOverCapacity) ...[
            const SizedBox(height: 6),
            const StatusBadge('Over capacity', tone: Tone.bad, icon: Icons.warning_rounded),
          ],
        ],
      ),
    );
  }
}

/// A-18 detail: occupancy per type, address, contact, hours; edit and (de)activate.
class LotDetailScreen extends ConsumerWidget {
  const LotDetailScreen({super.key, required this.id});

  final int id;

  void _reload(WidgetRef ref) => ref
    ..invalidate(lotProvider(id))
    ..invalidate(allLotsProvider);

  Future<void> _toggle(BuildContext context, WidgetRef ref, Lot lot) async {
    final sure = await confirmAction(
      context,
      title: lot.isActive ? 'Deactivate ${lot.name}?' : 'Activate ${lot.name}?',
      message: lot.isActive
          ? 'Staff can’t start trips from it or return vehicles to it, and transfers can’t go there.'
          : 'Staff can use it again.',
      confirmLabel: lot.isActive ? 'Deactivate' : 'Activate',
      danger: lot.isActive,
    );
    if (!sure) {
      return;
    }
    try {
      await ref.read(adminReposProvider).setLotActive(id, isActive: !lot.isActive, rowVersion: lot.rowVersion);
      _reload(ref);
    } on ApiException catch (error) {
      if (context.mounted) {
        showSaveError(context, error, onReload: () => _reload(ref));
      }
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final lot = ref.watch(lotProvider(id));
    return Scaffold(
      appBar: AppBar(
        title: const Text('Parking lot'),
        actions: [
          if (lot.hasValue)
            IconButton(
              tooltip: 'Edit',
              icon: const Icon(Icons.edit_rounded),
              onPressed: () async {
                await context.push('/lots/$id/edit');
                _reload(ref);
              },
            ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          _reload(ref);
          await ref.read(lotProvider(id).future);
        },
        child: AsyncBody(
          value: lot,
          onRetry: () => _reload(ref),
          data: (l) => ListView(
            padding: const EdgeInsets.fromLTRB(DmSize.gutter, 8, DmSize.gutter, 24),
            children: [
              LotCard(lot: l),
              const SectionLabel('By vehicle type'),
              AppCard(
                child: l.byType.isEmpty
                    ? Text('No capacity per type set.', style: context.text.bodyMedium)
                    : Column(
                        children: [
                          for (final t in l.byType)
                            KeyValueRow(
                              '${t.category} · ${t.type}',
                              t.capacity == null
                                  ? '${t.occupied} parked (no capacity set)'
                                  : '${t.occupied} of ${t.capacity} · ${t.available} free',
                            ),
                        ],
                      ),
              ),
              const SectionLabel('Details'),
              AppCard(
                child: Column(
                  children: [
                    KeyValueRow('Address', l.address),
                    KeyValueRow('Contact', l.contactName),
                    KeyValueRow(
                      'Contact number',
                      l.contactNumber,
                      trailing: InkWell(
                        onTap: () => copyText(context, l.contactNumber, what: 'Number copied'),
                        child: Text(l.contactNumber, style: context.text.titleSmall?.copyWith(color: context.dm.primary)),
                      ),
                    ),
                    KeyValueRow('Hours', l.hours),
                    KeyValueRow('Baseline (counted before go-live)', '${l.baselineOccupied}'),
                  ],
                ),
              ),
              const SizedBox(height: 14),
              OutlinedButton.icon(
                style: l.isActive ? OutlinedButton.styleFrom(foregroundColor: context.dm.dText) : null,
                onPressed: () => _toggle(context, ref, l),
                icon: Icon(l.isActive ? Icons.block_rounded : Icons.check_circle_rounded),
                label: Text(l.isActive ? 'Deactivate lot' : 'Activate lot'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// A-18 form: basics · contact and hours · capacity per vehicle type and baseline counts (LOT-01, LOT-02).
/// No location: no map in v1 (D-056).
class LotFormScreen extends ConsumerStatefulWidget {
  const LotFormScreen({super.key, this.id});

  final int? id;

  @override
  ConsumerState<LotFormScreen> createState() => _LotFormScreenState();
}

class _TypeRow {
  _TypeRow(this.option, {int? capacity, int baseline = 0})
    : capacity = TextEditingController(text: capacity?.toString() ?? ''),
      baseline = TextEditingController(text: '$baseline');

  final TypeOption option;
  final TextEditingController capacity;
  final TextEditingController baseline;

  void dispose() {
    capacity.dispose();
    baseline.dispose();
  }
}

class _LotFormScreenState extends ConsumerState<LotFormScreen> {
  final _form = GlobalKey<FormState>();
  final _code = TextEditingController();
  final _name = TextEditingController();
  final _address = TextEditingController();
  final _contactName = TextEditingController();
  final _contactNumber = TextEditingController();
  final _total = TextEditingController(text: '0');
  final _baseline = TextEditingController(text: '0');
  LotType _type = LotType.open;
  bool _allDay = true;
  TimeOfDay _opens = const TimeOfDay(hour: 6, minute: 0);
  TimeOfDay _closes = const TimeOfDay(hour: 22, minute: 0);
  List<_TypeRow> _types = [];
  Lot? _existing;
  ApiException? _error;
  bool _loaded = false;

  bool get _isEdit => widget.id != null;

  @override
  void initState() {
    super.initState();
    _load();
  }

  static TimeOfDay _parse(String value) {
    final [h, m] = value.split(':');
    return TimeOfDay(hour: int.parse(h), minute: int.parse(m));
  }

  static String _format(TimeOfDay t) => '${t.hour.toString().padLeft(2, '0')}:${t.minute.toString().padLeft(2, '0')}';

  Future<void> _load() async {
    try {
      final options = await ref.read(activeTypeOptionsProvider.future);
      final lot = _isEdit ? await ref.read(adminReposProvider).lot(widget.id!) : null;
      if (!mounted) {
        return;
      }
      setState(() {
        _existing = lot;
        final byType = {for (final t in lot?.byType ?? const <LotTypeOccupancy>[]) t.vehicleTypeId: t};
        for (final row in _types) {
          row.dispose();
        }
        _types = [
          for (final option in options)
            _TypeRow(option, capacity: byType[option.id]?.capacity, baseline: byType[option.id]?.baselineOccupied ?? 0),
        ];
        if (lot != null) {
          _code.text = lot.code;
          _name.text = lot.name;
          _address.text = lot.address;
          _contactName.text = lot.contactName;
          _contactNumber.text = lot.contactNumber;
          _total.text = '${lot.total.capacity}';
          _baseline.text = '${lot.baselineOccupied}';
          _type = lot.lotType;
          _allDay = lot.opensAt == null || lot.closesAt == null;
          if (!_allDay) {
            _opens = _parse(lot.opensAt!);
            _closes = _parse(lot.closesAt!);
          }
        }
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
    for (final c in [_code, _name, _address, _contactName, _contactNumber, _total, _baseline]) {
      c.dispose();
    }
    for (final row in _types) {
      row.dispose();
    }
    super.dispose();
  }

  Future<void> _save() async {
    setState(() => _error = null);
    if (!_form.currentState!.validate()) {
      return;
    }
    final input = LotInput(
      code: _code.text,
      name: _name.text,
      address: _address.text,
      contactName: _contactName.text,
      contactNumber: _contactNumber.text,
      opensAt: _allDay ? null : _format(_opens),
      closesAt: _allDay ? null : _format(_closes),
      lotType: _type,
      totalCapacity: int.parse(_total.text),
      baselineOccupied: int.parse(_baseline.text),
      typeCapacities: [
        for (final row in _types)
          if (row.capacity.text.trim().isNotEmpty)
            TypeCapacityInput(
              vehicleTypeId: row.option.id,
              capacity: int.parse(row.capacity.text),
              baselineOccupied: int.tryParse(row.baseline.text) ?? 0,
            ),
      ],
    );
    try {
      final saved = await ref.read(adminReposProvider).saveLot(input, id: widget.id, rowVersion: _existing?.rowVersion);
      ref
        ..invalidate(allLotsProvider)
        ..invalidate(lotProvider(saved.id))
        ..invalidate(dashboardProvider);
      if (mounted) {
        showMessage(context, _isEdit ? 'Lot saved.' : 'Lot added.');
        context.pop(true);
      }
    } on ApiException catch (error) {
      if (mounted) {
        setState(() => _error = error);
        showSaveError(context, error, onReload: _load);
      }
    }
  }

  Future<void> _pickTime(bool opens) async {
    final picked = await showTimePicker(context: context, initialTime: opens ? _opens : _closes);
    if (picked != null) {
      setState(() => opens ? _opens = picked : _closes = picked);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (!_loaded) {
      return Scaffold(
        appBar: AppBar(title: Text(_isEdit ? 'Edit lot' : 'Add lot')),
        body: _error == null ? const SkeletonList() : ErrorView(error: _error!, onRetry: _load),
      );
    }
    final typeSum = _types.fold<int>(0, (sum, row) => sum + (int.tryParse(row.capacity.text) ?? 0));
    return FlowScaffold(
      title: _isEdit ? 'Edit lot' : 'Add lot',
      bottom: BusyButton(label: _isEdit ? 'Save' : 'Add lot', onPressed: _save),
      children: [
        Form(
          key: _form,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              FormSection(
                title: 'Basics',
                children: [
                  TextFormField(
                    controller: _code,
                    textCapitalization: TextCapitalization.characters,
                    inputFormatters: [FilteringTextInputFormatter.allow(RegExp('[A-Za-z0-9-]'))],
                    decoration: InputDecoration(labelText: 'Code (e.g. AQD)', errorText: _error?.fieldError('code')),
                    validator: (v) => (v ?? '').trim().isEmpty ? 'Enter a lot code.' : null,
                  ),
                  TextFormField(
                    controller: _name,
                    textCapitalization: TextCapitalization.words,
                    decoration: InputDecoration(labelText: 'Name', errorText: _error?.fieldError('name')),
                    validator: (v) => (v ?? '').trim().isEmpty ? 'Enter a name.' : null,
                  ),
                  TextFormField(
                    controller: _address,
                    maxLines: 2,
                    decoration: InputDecoration(labelText: 'Full address', errorText: _error?.fieldError('address')),
                    validator: (v) => (v ?? '').trim().isEmpty ? 'Enter the address.' : null,
                  ),
                  DropdownButtonFormField<LotType>(
                    initialValue: _type,
                    decoration: InputDecoration(labelText: 'Lot type', errorText: _error?.fieldError('lotType')),
                    items: [for (final t in LotType.values) DropdownMenuItem(value: t, child: Text(lotTypeLabel(t)))],
                    onChanged: (v) => setState(() => _type = v ?? LotType.open),
                  ),
                ],
              ),
              FormSection(
                title: 'Contact and hours',
                children: [
                  TextFormField(
                    controller: _contactName,
                    textCapitalization: TextCapitalization.words,
                    decoration: InputDecoration(labelText: 'Contact person', errorText: _error?.fieldError('contactName')),
                    validator: (v) => (v ?? '').trim().isEmpty ? 'Enter a contact person.' : null,
                  ),
                  TextFormField(
                    controller: _contactNumber,
                    keyboardType: TextInputType.phone,
                    decoration: InputDecoration(labelText: 'Contact number', errorText: _error?.fieldError('contactNumber')),
                    validator: (v) => (v ?? '').trim().isEmpty ? 'Enter a contact number.' : null,
                  ),
                  SwitchListTile(
                    contentPadding: EdgeInsets.zero,
                    title: const Text('Open 24 hours'),
                    value: _allDay,
                    onChanged: (v) => setState(() => _allDay = v),
                  ),
                  if (!_allDay)
                    Row(
                      children: [
                        Expanded(
                          child: OutlinedButton(onPressed: () => _pickTime(true), child: Text('Opens ${_format(_opens)}')),
                        ),
                        const SizedBox(width: 10),
                        Expanded(
                          child: OutlinedButton(onPressed: () => _pickTime(false), child: Text('Closes ${_format(_closes)}')),
                        ),
                      ],
                    ),
                  if (_error?.fieldError('opensAt') != null || _error?.fieldError('closesAt') != null)
                    Text(
                      _error?.fieldError('opensAt') ?? _error!.fieldError('closesAt')!,
                      style: TextStyle(color: context.dm.dText),
                    ),
                ],
              ),
              FormSection(
                title: 'Capacity',
                children: [
                  NumberField(controller: _total, label: 'Total capacity', errorText: _error?.fieldError('totalCapacity')),
                  NumberField(
                    controller: _baseline,
                    label: 'Already parked at go-live',
                    helperText: 'Vehicles not in the system that take spaces',
                    errorText: _error?.fieldError('baselineOccupied'),
                  ),
                ],
              ),
              SectionLabel('Capacity per vehicle type (leave empty for none) · $typeSum set'),
              if (_types.isEmpty)
                Text('Add vehicle categories and types first.', style: context.text.bodyMedium)
              else
                AppCard(
                  child: Column(
                    children: [
                      for (final row in _types)
                        Padding(
                          padding: const EdgeInsets.symmetric(vertical: 6),
                          child: Row(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Expanded(
                                flex: 3,
                                child: Padding(
                                  padding: const EdgeInsets.only(top: 14),
                                  child: Text(row.option.label, style: context.text.bodyMedium),
                                ),
                              ),
                              const SizedBox(width: 8),
                              Expanded(
                                flex: 2,
                                child: TextFormField(
                                  controller: row.capacity,
                                  keyboardType: TextInputType.number,
                                  inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                                  onChanged: (_) => setState(() {}),
                                  decoration: const InputDecoration(labelText: 'Spaces'),
                                ),
                              ),
                              const SizedBox(width: 8),
                              Expanded(
                                flex: 2,
                                child: TextFormField(
                                  controller: row.baseline,
                                  keyboardType: TextInputType.number,
                                  inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                                  decoration: const InputDecoration(labelText: 'Parked'),
                                ),
                              ),
                            ],
                          ),
                        ),
                      if (_error?.fieldError('typeCapacities') != null)
                        Text(_error!.fieldError('typeCapacities')!, style: TextStyle(color: context.dm.dText)),
                    ],
                  ),
                ),
            ],
          ),
        ),
      ],
    );
  }
}
