import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../state.dart';
import 'common.dart';
import 'scan.dart';
import 'trip_view.dart';

/// S-10 → S-11 → S-12 → S-13: verify the vehicle, pick the return lot, confirm, see the summary (TRP-09, TRP-10).
class EndTripScreen extends ConsumerStatefulWidget {
  const EndTripScreen({super.key});

  @override
  ConsumerState<EndTripScreen> createState() => _EndTripScreenState();
}

class _EndTripScreenState extends ConsumerState<EndTripScreen> {
  ScanResult? _scan;
  int? _lotId;
  bool _checking = false;

  Future<void> _verify(TripDetail trip) async {
    final result = await scanVehicle(context, purpose: 'Scan the vehicle you return');
    if (result == null || !mounted) {
      return;
    }
    setState(() => _checking = true);
    try {
      final lookup = await ref.read(reposProvider).lookupVehicle(qr: result.qr, registration: result.registration);
      if (!mounted) {
        return;
      }
      if (lookup.vehicle.id != trip.currentVehicle.id) {
        showMessage(
          context,
          'That is ${lookup.vehicle.registrationNumber}. This trip uses ${trip.currentVehicle.registrationNumber}.',
        );
        return;
      }
      setState(() {
        _scan = result;
        _lotId ??= trip.originLot.id;
      });
    } on ApiException catch (error) {
      if (mounted) {
        showError(context, error);
      }
    } finally {
      if (mounted) {
        setState(() => _checking = false);
      }
    }
  }

  Future<void> _confirm(TripDetail trip, Lot lot) async {
    final confirmed = await showModalBottomSheet<bool>(
      context: context,
      builder: (context) => Padding(
        padding: const EdgeInsets.fromLTRB(DmSize.gutter, 0, DmSize.gutter, 28),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('End this trip?', style: context.text.titleLarge),
            const SizedBox(height: 12),
            KeyValueRow('Vehicle', trip.currentVehicle.registrationNumber),
            KeyValueRow('Return lot', lot.name),
            Ticker(
              builder: (_, now) => KeyValueRow('Duration', formatDuration(now.difference(trip.startedAtUtc))),
            ),
            const SizedBox(height: 16),
            FilledButton.icon(
              onPressed: () => Navigator.of(context).pop(true),
              icon: const Icon(Icons.flag_rounded),
              label: const Text('End trip'),
            ),
            const SizedBox(height: 8),
            OutlinedButton(onPressed: () => Navigator.of(context).pop(false), child: const Text('Not yet')),
          ],
        ),
      ),
    );
    if (confirmed != true || !mounted) {
      return;
    }
    try {
      final ended = await ref.read(reposProvider).endTrip(
        trip.id,
        vehicleQrToken: _scan!.qr,
        vehicleRegistration: _scan!.registration,
        returnLotId: lot.id,
      );
      ref.read(activeTripProvider.notifier).set(null);
      refreshAfterTripAction(ref);
      if (mounted) {
        // Only the id goes through navigation (in-memory extras can be lost when the router refreshes);
        // the warnings exist only in this response, so they ride along and are optional.
        context.pushReplacement('/trip/summary/${ended.id}', extra: ended.warnings);
      }
    } on ApiException catch (error) {
      if (mounted) {
        showError(context, error);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final trip = ref.watch(activeTripProvider).valueOrNull;
    if (trip == null) {
      return const FlowScaffold(
        title: 'End trip',
        children: [EmptyView(icon: Icons.directions_car_rounded, title: 'No trip running', message: 'There is nothing to end.')],
      );
    }
    return _scan == null ? _verifyStep(trip) : _lotStep(trip);
  }

  Widget _verifyStep(TripDetail trip) => FlowScaffold(
    title: 'End trip',
    step: 1,
    totalSteps: 2,
    bottom: FilledButton.icon(
      onPressed: _checking ? null : () => _verify(trip),
      icon: _checking
          ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2.4))
          : const Icon(Icons.qr_code_scanner_rounded),
      label: const Text('Scan vehicle'),
    ),
    children: [
      Text('Check the vehicle', style: context.text.titleLarge),
      const SizedBox(height: 6),
      Text('Scan the QR sticker (or type the registration) of the vehicle you are returning.', style: context.text.bodyMedium),
      const SizedBox(height: 16),
      AppCard(
        child: Row(
          children: [
            PlateBadge(trip.currentVehicle.registrationNumber, large: true),
            const SizedBox(width: 12),
            Expanded(child: Text('${trip.currentVehicle.brand} ${trip.currentVehicle.model}', style: context.text.titleSmall)),
          ],
        ),
      ),
    ],
  );

  Widget _lotStep(TripDetail trip) {
    final lots = ref.watch(lotsProvider);
    return FlowScaffold(
      title: 'End trip',
      step: 2,
      totalSteps: 2,
      onBack: () => setState(() => _scan = null),
      bottom: BusyButton(
        label: 'Continue',
        icon: Icons.arrow_forward_rounded,
        onPressed: _lotId == null
            ? null
            : () async {
                final lot = lots.valueOrNull?.firstWhere((l) => l.id == _lotId);
                if (lot != null) {
                  await _confirm(trip, lot);
                }
              },
      ),
      children: [
        Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(color: context.dm.pSoft, borderRadius: BorderRadius.circular(16)),
          child: Row(
            children: [
              Icon(Icons.check_circle_rounded, color: context.dm.onPSoft),
              const SizedBox(width: 8),
              Text('Vehicle matched', style: dmStyle(14, FontWeight.w700, color: context.dm.onPSoft)),
            ],
          ),
        ),
        const SizedBox(height: 16),
        Text('Where is it parked now?', style: context.text.titleLarge),
        const SizedBox(height: 12),
        AsyncBody(
          value: lots,
          onRetry: () => ref.invalidate(lotsProvider),
          data: (items) => Column(
            children: [
              for (final lot in items) ...[
                AppCard(
                  selected: _lotId == lot.id,
                  onTap: () => setState(() => _lotId = lot.id),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Icon(
                            _lotId == lot.id ? Icons.radio_button_checked_rounded : Icons.radio_button_unchecked_rounded,
                            color: context.dm.primary,
                          ),
                          const SizedBox(width: 10),
                          Expanded(child: Text(lot.name, style: context.text.titleSmall)),
                          if (lot.id == trip.originLot.id) const StatusBadge('Origin', tone: Tone.info),
                          if (lot.total.isFull) ...[
                            const SizedBox(width: 6),
                            const StatusBadge('Full', tone: Tone.warn, icon: Icons.warning_rounded),
                          ],
                        ],
                      ),
                      const SizedBox(height: 8),
                      CapacityBar(fill: lot.total.fill, isFull: lot.total.isFull),
                      const SizedBox(height: 4),
                      Text('${lot.total.available} free of ${lot.total.capacity}', style: context.text.bodySmall),
                    ],
                  ),
                ),
                const SizedBox(height: 8),
              ],
            ],
          ),
        ),
      ],
    );
  }
}

/// S-13: total time and the trip facts after ending. Loads the trip by id, so it survives router refreshes.
class TripSummaryScreen extends ConsumerWidget {
  const TripSummaryScreen({super.key, required this.id, this.warnings = const []});

  final int id;

  /// Non-blocking warnings from the end call (e.g. the return lot is now full).
  final List<Notice> warnings;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final trip = ref.watch(tripDetailProvider(id));
    return Scaffold(
      appBar: AppBar(
        automaticallyImplyLeading: false,
        actions: [IconButton(onPressed: () => context.go('/home'), icon: const Icon(Icons.close_rounded))],
      ),
      body: SafeArea(
        child: AsyncBody(
          value: trip,
          onRetry: () => ref.invalidate(tripDetailProvider(id)),
          data: (detail) => ListView(
            padding: const EdgeInsets.fromLTRB(DmSize.gutter, 0, DmSize.gutter, 24),
            children: [
              const Center(child: IconTile(Icons.task_alt_rounded, size: 64)),
              const SizedBox(height: 12),
              Center(
                child: Text(
                  formatDuration(Duration(seconds: detail.totalSeconds ?? 0)),
                  style: dmStyle(44, FontWeight.w800, color: context.dm.text, tabular: true, spacing: -1.5),
                ),
              ),
              Center(child: Text('Total time outside the lot', style: context.text.bodyMedium)),
              const SizedBox(height: 18),
              WarningsBanner(warnings: warnings),
              TripFactsCard(trip: detail),
              const SizedBox(height: 16),
              FilledButton(onPressed: () => context.go('/home'), child: const Text('Done')),
            ],
          ),
        ),
      ),
    );
  }
}
