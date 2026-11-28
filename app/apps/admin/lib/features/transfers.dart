import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../state.dart';
import '../widgets/admin_widgets.dart';

/// A-07: a transfer with its timeline. Admins can cancel it only before departure (TRF-06); staff drive it.
class TransferDetailScreen extends ConsumerWidget {
  const TransferDetailScreen({super.key, required this.id});

  final int id;

  Future<void> _cancel(BuildContext context, WidgetRef ref, Transfer t) async {
    final sure = await confirmAction(
      context,
      title: 'Cancel this transfer?',
      message: '${t.vehicle.registrationNumber} stays at ${t.fromLot.name}. Staff no longer see the task.',
      confirmLabel: 'Cancel transfer',
      cancelLabel: 'Keep',
      danger: true,
    );
    if (!sure) {
      return;
    }
    try {
      await ref.read(adminReposProvider).cancelTransfer(id);
      ref
        ..invalidate(transferProvider(id))
        ..invalidate(openTransfersProvider)
        ..invalidate(dashboardProvider);
      if (context.mounted) {
        showMessage(context, 'Transfer cancelled.');
      }
    } on ApiException catch (error) {
      if (context.mounted) {
        showError(context, error);
      }
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final transfer = ref.watch(transferProvider(id));
    return Scaffold(
      appBar: AppBar(title: Text('Transfer #$id')),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(transferProvider(id));
          await ref.read(transferProvider(id).future);
        },
        child: AsyncBody(
          value: transfer,
          onRetry: () => ref.invalidate(transferProvider(id)),
          data: (t) => ListView(
            padding: const EdgeInsets.fromLTRB(DmSize.gutter, 8, DmSize.gutter, 24),
            children: [
              WarningsBanner(warnings: t.warnings),
              TransferFactsCard(transfer: t),
              const SizedBox(height: 16),
              if (t.status == TransferStatus.pendingDeparture) ...[
                Text('Any staff member can confirm departure in the staff app; whoever does drives it.', style: context.text.bodySmall),
                const SizedBox(height: 10),
                BusyButton(
                  label: 'Cancel transfer',
                  icon: Icons.cancel_rounded,
                  danger: true,
                  outlined: true,
                  onPressed: () => _cancel(context, ref, t),
                ),
              ],
              if (t.status == TransferStatus.inTransit)
                Text('On the way. Any staff member confirms arrival at ${t.toLot.name}.', style: context.text.bodySmall),
            ],
          ),
        ),
      ),
    );
  }
}

/// A-06: pick an available vehicle parked at a lot, then the destination lot, optional note. No driver: the staff
/// member who confirms departure drives (D-020, D-047).
class NewTransferScreen extends ConsumerStatefulWidget {
  const NewTransferScreen({super.key, this.vehicle});

  /// Preselected (from a vehicle's detail screen).
  final VehicleDetail? vehicle;

  @override
  ConsumerState<NewTransferScreen> createState() => _NewTransferScreenState();
}

class _NewTransferScreenState extends ConsumerState<NewTransferScreen> {
  late VehicleDetail? _vehicle = widget.vehicle;
  int? _toLotId;
  final _note = TextEditingController();
  ApiException? _error;

  @override
  void dispose() {
    _note.dispose();
    super.dispose();
  }

  Future<void> _pickVehicle() async {
    final picked = await showModalBottomSheet<VehicleDetail>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (_) => const _VehiclePicker(),
    );
    if (picked != null) {
      setState(() {
        _vehicle = picked;
        if (_toLotId == picked.currentLot?.id) {
          _toLotId = null;
        }
      });
    }
  }

  Future<void> _create() async {
    setState(() => _error = null);
    final vehicle = _vehicle;
    if (vehicle == null || _toLotId == null) {
      showMessage(context, vehicle == null ? 'Pick a vehicle.' : 'Pick the destination lot.');
      return;
    }
    try {
      final transfer = await ref
          .read(adminReposProvider)
          .createTransfer(vehicleId: vehicle.id, toLotId: _toLotId!, note: _note.text);
      ref
        ..invalidate(openTransfersProvider)
        ..invalidate(dashboardProvider);
      if (mounted) {
        showMessage(context, 'Transfer created. All staff are notified.');
        context.pushReplacement('/transfers/${transfer.id}');
      }
    } on ApiException catch (error) {
      if (mounted) {
        setState(() => _error = error);
        showSaveError(context, error);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final lots = ref.watch(activeLotsProvider);
    final vehicle = _vehicle;
    return FlowScaffold(
      title: 'New transfer',
      bottom: BusyButton(label: 'Create transfer', icon: Icons.local_shipping_rounded, onPressed: _create),
      children: [
        const SectionLabel('Vehicle'),
        if (vehicle == null)
          OutlinedButton.icon(onPressed: _pickVehicle, icon: const Icon(Icons.search_rounded), label: const Text('Pick a vehicle'))
        else
          ListRow(
            leading: PlateBadge(vehicle.registrationNumber),
            title: '${vehicle.category} · ${vehicle.type}',
            subtitle: 'At ${vehicle.currentLot?.name ?? '—'} · tap to change',
            onTap: _pickVehicle,
          ),
        if (_error?.fieldError('vehicleId') != null)
          Padding(
            padding: const EdgeInsets.only(top: 6),
            child: Text(_error!.fieldError('vehicleId')!, style: TextStyle(color: context.dm.dText)),
          ),
        const SectionLabel('To lot'),
        AsyncBody(
          value: lots,
          onRetry: () => ref.invalidate(allLotsProvider),
          loading: const SkeletonList(count: 2, height: 60),
          data: (items) {
            final choices = items.where((l) => l.id != vehicle?.currentLot?.id).toList();
            if (choices.isEmpty) {
              return const EmptyView(icon: Icons.local_parking_rounded, title: 'No other lot', message: 'Add another active lot first.');
            }
            return Column(
              children: [
                for (final lot in choices) ...[
                  AppCard(
                    selected: _toLotId == lot.id,
                    onTap: () => setState(() => _toLotId = lot.id),
                    padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
                    child: Row(
                      children: [
                        Icon(
                          _toLotId == lot.id ? Icons.radio_button_checked_rounded : Icons.radio_button_unchecked_rounded,
                          color: context.dm.primary,
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(lot.name, style: context.text.titleSmall),
                              Text('${lot.total.available} free of ${lot.total.capacity}', style: context.text.bodySmall),
                            ],
                          ),
                        ),
                        if (lot.total.isFull) const StatusBadge('Full', tone: Tone.warn, icon: Icons.warning_rounded),
                      ],
                    ),
                  ),
                  const SizedBox(height: 8),
                ],
              ],
            );
          },
        ),
        if (_error?.fieldError('toLotId') != null)
          Text(_error!.fieldError('toLotId')!, style: TextStyle(color: context.dm.dText)),
        const SectionLabel('Note (optional)'),
        TextField(
          controller: _note,
          maxLength: 500,
          maxLines: 3,
          decoration: InputDecoration(hintText: 'e.g. needed at the depot by noon', errorText: _error?.fieldError('note')),
        ),
      ],
    );
  }
}

/// Search sheet: available vehicles parked at a lot (the only ones a transfer can move).
class _VehiclePicker extends ConsumerStatefulWidget {
  const _VehiclePicker();

  @override
  ConsumerState<_VehiclePicker> createState() => _VehiclePickerState();
}

class _VehiclePickerState extends ConsumerState<_VehiclePicker> {
  String _search = '';

  @override
  Widget build(BuildContext context) => SizedBox(
    height: MediaQuery.sizeOf(context).height * .85,
    child: Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(DmSize.gutter, 8, DmSize.gutter, 8),
          child: SearchField(hint: 'Registration, brand or model', onChanged: (v) => setState(() => _search = v)),
        ),
        Expanded(
          child: PagedListView<VehicleDetail>(
            key: ValueKey(_search),
            fetch: (page) => ref
                .read(adminReposProvider)
                .vehicles(search: _search, status: VehicleStatus.available, page: page),
            empty: const EmptyView(
              icon: Icons.directions_car_rounded,
              title: 'No vehicle found',
              message: 'Only available vehicles parked at a lot can be transferred.',
            ),
            itemBuilder: (context, v) => ListRow(
              leading: PlateBadge(v.registrationNumber),
              title: '${v.brand} ${v.model}',
              subtitle: '${v.category} · ${v.type} · ${v.currentLot?.name ?? 'Not at a lot'}',
              flat: v.currentLot == null,
              onTap: v.currentLot == null ? null : () => Navigator.of(context).pop(v),
            ),
          ),
        ),
      ],
    ),
  );
}
