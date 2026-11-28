import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../state.dart';

/// S-16 My trips: Today / This week / Month, grouped by day, the running trip pinned at the top (TRP-12).
class TripsScreen extends ConsumerStatefulWidget {
  const TripsScreen({super.key});

  @override
  ConsumerState<TripsScreen> createState() => _TripsScreenState();
}

class _TripsScreenState extends ConsumerState<TripsScreen> {
  TripRange _range = TripRange.today;

  @override
  Widget build(BuildContext context) {
    final trips = ref.watch(tripsProvider(_range));
    final active = ref.watch(activeTripProvider).valueOrNull;
    final me = ref.watch(meProvider);
    return RefreshIndicator(
      onRefresh: () async {
        ref.invalidate(tripsProvider(_range));
        await ref.read(tripsProvider(_range).future);
      },
      child: ListView(
        padding: const EdgeInsets.fromLTRB(DmSize.gutter, 12, DmSize.gutter, 24),
        children: [
          Text('My trips', style: context.text.headlineMedium),
          const SizedBox(height: 12),
          SegmentedButton<TripRange>(
            showSelectedIcon: false,
            segments: const [
              ButtonSegment(value: TripRange.today, label: Text('Today')),
              ButtonSegment(value: TripRange.week, label: Text('This week')),
              ButtonSegment(value: TripRange.month, label: Text('Month')),
            ],
            selected: {_range},
            onSelectionChanged: (value) => setState(() => _range = value.first),
          ),
          const SizedBox(height: 14),
          if (active != null) ...[
            AppCard(
              onTap: () => context.push('/trip'),
              child: Row(
                children: [
                  const IconTile(Icons.radio_button_checked_rounded, tone: Tone.live),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('Running now', style: context.text.titleSmall),
                        Text(
                          '${active.currentVehicle.registrationNumber} → ${active.destination.location}',
                          style: context.text.bodySmall,
                        ),
                      ],
                    ),
                  ),
                  Ticker(
                    builder: (_, now) => Text(
                      formatClock(now.difference(active.startedAtUtc)),
                      style: dmStyle(15, FontWeight.w800, color: context.dm.text, tabular: true),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 12),
          ],
          AsyncBody(
            value: trips,
            onRetry: () => ref.invalidate(tripsProvider(_range)),
            data: (items) {
              // Your own running trip is pinned above; a trip you handed over keeps running under someone else
              // and is listed here with "Handed over".
              final done = items.where((t) => !(t.status == TripStatus.active && t.currentStaff.id == me.id)).toList();
              if (done.isEmpty) {
                return const EmptyView(
                  icon: Icons.route_rounded,
                  title: 'No trips yet',
                  message: 'Trips you take part in show here.',
                );
              }
              final groups = <String, List<TripSummary>>{};
              for (final trip in done) {
                groups.putIfAbsent(Gst.dayLabel(trip.startedAtUtc), () => []).add(trip);
              }
              return Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  for (final group in groups.entries) ...[
                    SectionLabel(group.key),
                    for (final trip in group.value) ...[_TripRow(trip: trip), const SizedBox(height: 8)],
                  ],
                ],
              );
            },
          ),
        ],
      ),
    );
  }
}

class _TripRow extends ConsumerWidget {
  const _TripRow({required this.trip});

  final TripSummary trip;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final me = ref.watch(meProvider);
    final handedOver = trip.currentStaff.id != me.id;
    return AppCard(
      padding: const EdgeInsets.all(12),
      onTap: () => context.push('/trips/${trip.id}'),
      child: Row(
        children: [
          IconTile(trip.status == TripStatus.cancelled ? Icons.cancel_rounded : Icons.local_shipping_rounded, tone: Tone.neutral, size: 40),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(trip.currentVehicle.registrationNumber, style: context.text.titleSmall),
                Text(
                  '${Gst.time(trip.startedAtUtc)} · ${trip.originLot.name} → ${trip.destination.location}',
                  style: context.text.bodySmall,
                  overflow: TextOverflow.ellipsis,
                ),
              ],
            ),
          ),
          const SizedBox(width: 8),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              if (handedOver)
                const StatusBadge('Handed over', tone: Tone.info, icon: Icons.swap_horiz_rounded)
              else
                tripStatusBadge(trip.status),
              const SizedBox(height: 4),
              if (trip.totalSeconds != null)
                Text(formatDuration(Duration(seconds: trip.totalSeconds!)), style: context.text.bodySmall),
            ],
          ),
        ],
      ),
    );
  }
}

/// S-17: read-only trip with its segment timeline.
class TripDetailScreen extends ConsumerWidget {
  const TripDetailScreen({super.key, required this.id});

  final int id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final trip = ref.watch(tripDetailProvider(id));
    return Scaffold(
      appBar: AppBar(title: Text('Trip #$id')),
      body: AsyncBody(
        value: trip,
        onRetry: () => ref.invalidate(tripDetailProvider(id)),
        data: (detail) => ListView(
          padding: const EdgeInsets.fromLTRB(DmSize.gutter, 8, DmSize.gutter, 24),
          children: [
            TripFactsCard(trip: detail),
            const SizedBox(height: 14),
            AppCard(child: SegmentTimeline(segments: detail.segments)),
          ],
        ),
      ),
    );
  }
}
