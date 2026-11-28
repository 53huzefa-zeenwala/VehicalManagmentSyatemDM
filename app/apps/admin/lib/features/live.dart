import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../shell.dart';
import '../state.dart';

enum LiveView { trips, transfers }

/// Which Live view is showing; other tabs can switch it (e.g. the dashboard's "See all").
final liveViewProvider = StateProvider<LiveView>((ref) => LiveView.trips);

/// Opens the Live tab on [view].
void showLiveTab(BuildContext context, LiveView view) {
  ProviderScope.containerOf(context).read(liveViewProvider.notifier).state = view;
  context.go('/live');
}

/// A-02 / A-03: everything moving now. Trips (overdue first, timers ticking on the phone) and open transfers.
/// Refreshes on open and on pull, no auto-updates (D-035).
class LiveScreen extends ConsumerWidget {
  const LiveScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final view = ref.watch(liveViewProvider);
    return Scaffold(
      appBar: TabAppBar(
        title: 'Live',
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(56),
          child: Padding(
            padding: const EdgeInsets.fromLTRB(DmSize.gutter, 0, DmSize.gutter, 10),
            child: SizedBox(
              width: double.infinity,
              child: SegmentedButton<LiveView>(
                showSelectedIcon: false,
                segments: const [
                  ButtonSegment(value: LiveView.trips, label: Text('Trips'), icon: Icon(Icons.route_rounded)),
                  ButtonSegment(value: LiveView.transfers, label: Text('Transfers'), icon: Icon(Icons.local_shipping_rounded)),
                ],
                selected: {view},
                onSelectionChanged: (value) => ref.read(liveViewProvider.notifier).state = value.first,
              ),
            ),
          ),
        ),
      ),
      floatingActionButton: view == LiveView.transfers
          ? FloatingActionButton.extended(
              onPressed: () async {
                await context.push('/transfers/new');
                ref.invalidate(openTransfersProvider);
              },
              icon: const Icon(Icons.add_rounded),
              label: const Text('New transfer'),
            )
          : null,
      body: switch (view) {
        LiveView.trips => const _LiveTrips(),
        LiveView.transfers => const _LiveTransfers(),
      },
    );
  }
}

class _LiveTrips extends ConsumerWidget {
  const _LiveTrips();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final trips = ref.watch(liveTripsProvider);
    final filter = ref.watch(liveFilterProvider);
    final lots = ref.watch(allLotsProvider).valueOrNull ?? const [];
    final types = ref.watch(activeTypeOptionsProvider).valueOrNull ?? const [];
    return RefreshIndicator(
      onRefresh: () async {
        ref.invalidate(liveTripsProvider);
        await ref.read(liveTripsProvider.future);
      },
      child: ListView(
        padding: const EdgeInsets.fromLTRB(DmSize.gutter, 4, DmSize.gutter, 24),
        children: [
          Row(
            children: [
              Expanded(
                child: DropdownButtonFormField<int?>(
                  initialValue: filter.lotId,
                  isExpanded: true,
                  decoration: const InputDecoration(labelText: 'Started from'),
                  items: [
                    const DropdownMenuItem(value: null, child: Text('All lots')),
                    for (final lot in lots) DropdownMenuItem(value: lot.id, child: Text(lot.name)),
                  ],
                  onChanged: (value) =>
                      ref.read(liveFilterProvider.notifier).state = LiveFilter(lotId: value, typeId: filter.typeId),
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: DropdownButtonFormField<int?>(
                  initialValue: filter.typeId,
                  isExpanded: true,
                  decoration: const InputDecoration(labelText: 'Vehicle type'),
                  items: [
                    const DropdownMenuItem(value: null, child: Text('All types')),
                    for (final type in types) DropdownMenuItem(value: type.id, child: Text(type.label)),
                  ],
                  onChanged: (value) =>
                      ref.read(liveFilterProvider.notifier).state = LiveFilter(lotId: filter.lotId, typeId: value),
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          AsyncBody(
            value: trips,
            onRetry: () => ref.invalidate(liveTripsProvider),
            data: (items) => items.isEmpty
                ? const EmptyView(icon: Icons.route_rounded, title: 'No trips running', message: 'Trips show here while they run.')
                : Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Text('${items.length} running', style: context.text.labelMedium),
                      const SizedBox(height: 8),
                      for (final trip in items) ...[TripRow(trip: trip), const SizedBox(height: 8)],
                    ],
                  ),
          ),
        ],
      ),
    );
  }
}

/// A trip in a list: plate, driver, route, and a ticking timer while it runs.
class TripRow extends StatelessWidget {
  const TripRow({super.key, required this.trip});

  final TripSummary trip;

  @override
  Widget build(BuildContext context) {
    final running = trip.status == TripStatus.active;
    return AppCard(
      onTap: () => context.push('/trips/${trip.id}'),
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              PlateBadge(trip.currentVehicle.registrationNumber),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  '${trip.currentVehicle.category} · ${trip.currentVehicle.type}',
                  style: context.text.bodySmall,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              tripStatusBadge(trip.status, overdue: trip.isOverdue),
            ],
          ),
          const SizedBox(height: 8),
          Text(trip.currentStaff.fullName, style: context.text.titleSmall),
          Text('${trip.originLot.name} → ${trip.destination.label}', style: context.text.bodySmall),
          const SizedBox(height: 4),
          Row(
            children: [
              Text(Gst.dateTime(trip.startedAtUtc), style: context.text.labelSmall),
              if (trip.segmentCount > 1) ...[
                const SizedBox(width: 8),
                Text('· ${trip.segmentCount - 1} change${trip.segmentCount > 2 ? 's' : ''}', style: context.text.labelSmall),
              ],
              const Spacer(),
              if (running)
                Ticker(
                  builder: (_, now) => Text(
                    formatClock(now.difference(trip.startedAtUtc)),
                    style: dmStyle(15, FontWeight.w800, color: trip.isOverdue ? context.dm.danger : context.dm.text, tabular: true),
                  ),
                )
              else if (trip.totalSeconds != null)
                Text(formatDuration(Duration(seconds: trip.totalSeconds!)), style: context.text.titleSmall),
            ],
          ),
        ],
      ),
    );
  }
}

class _LiveTransfers extends ConsumerWidget {
  const _LiveTransfers();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final transfers = ref.watch(openTransfersProvider);
    return RefreshIndicator(
      onRefresh: () async {
        ref.invalidate(openTransfersProvider);
        await ref.read(openTransfersProvider.future);
      },
      child: AsyncBody(
        value: transfers,
        onRetry: () => ref.invalidate(openTransfersProvider),
        data: (data) {
          final (waiting, onTheWay) = data;
          if (waiting.isEmpty && onTheWay.isEmpty) {
            return ListView(
              children: const [
                EmptyView(
                  icon: Icons.local_shipping_rounded,
                  title: 'No open transfers',
                  message: 'Create a transfer to move a vehicle between lots. Any staff member can drive it.',
                ),
              ],
            );
          }
          return ListView(
            padding: const EdgeInsets.fromLTRB(DmSize.gutter, 4, DmSize.gutter, 96),
            children: [
              if (onTheWay.isNotEmpty) ...[
                SectionLabel('On the way (${onTheWay.length})'),
                for (final t in onTheWay) ...[TransferRow(transfer: t), const SizedBox(height: 8)],
              ],
              if (waiting.isNotEmpty) ...[
                SectionLabel('Waiting to depart (${waiting.length})'),
                for (final t in waiting) ...[TransferRow(transfer: t), const SizedBox(height: 8)],
              ],
            ],
          );
        },
      ),
    );
  }
}

/// A transfer in a list: plate, route, status, driver and driving time.
class TransferRow extends StatelessWidget {
  const TransferRow({super.key, required this.transfer});

  final Transfer transfer;

  @override
  Widget build(BuildContext context) {
    final t = transfer;
    return AppCard(
      onTap: () => context.push('/transfers/${t.id}'),
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              PlateBadge(t.vehicle.registrationNumber),
              const Spacer(),
              transferStatusBadge(t.status, overdue: t.isOverdue),
            ],
          ),
          const SizedBox(height: 8),
          Text('${t.fromLot.name} → ${t.toLot.name}', style: context.text.titleSmall),
          Row(
            children: [
              Expanded(
                child: Text(
                  t.driver == null ? 'Created ${Gst.dateTime(t.createdAtUtc)}' : 'Driver ${t.driver!.fullName}',
                  style: context.text.bodySmall,
                ),
              ),
              if (t.status == TransferStatus.inTransit && t.departedAtUtc != null)
                Ticker(
                  builder: (_, now) =>
                      Text(formatClock(now.difference(t.departedAtUtc!)), style: dmStyle(14, FontWeight.w800, tabular: true)),
                )
              else if (t.drivingSeconds != null)
                Text(formatDuration(Duration(seconds: t.drivingSeconds!)), style: context.text.titleSmall),
            ],
          ),
        ],
      ),
    );
  }
}
