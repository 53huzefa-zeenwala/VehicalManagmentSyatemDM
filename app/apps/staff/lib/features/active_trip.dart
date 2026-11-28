import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../state.dart';

/// S-09: running trip — timer ring, vehicle and route, segment timeline, End / Swap / Cancel.
class ActiveTripScreen extends ConsumerWidget {
  const ActiveTripScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final trip = ref.watch(activeTripProvider);
    return Scaffold(
      appBar: AppBar(title: const Text('Active trip')),
      body: RefreshIndicator(
        onRefresh: () => ref.read(activeTripProvider.notifier).refresh(),
        child: AsyncBody(
          value: trip,
          onRetry: () => ref.read(activeTripProvider.notifier).refresh(),
          data: (active) => active == null
              ? ListView(
                  children: [
                    EmptyView(
                      icon: Icons.directions_car_rounded,
                      title: 'No trip running',
                      message: 'Scan a vehicle to start one.',
                      action: FilledButton(onPressed: () => context.go('/home'), child: const Text('Go home')),
                    ),
                  ],
                )
              : _ActiveTripBody(trip: active),
        ),
      ),
    );
  }
}

class _ActiveTripBody extends StatelessWidget {
  const _ActiveTripBody({required this.trip});

  final TripDetail trip;

  @override
  Widget build(BuildContext context) {
    final c = context.dm;
    final ringColor = trip.isOverdue ? c.danger : c.primary;
    return ListView(
      padding: const EdgeInsets.fromLTRB(DmSize.gutter, 8, DmSize.gutter, 24),
      children: [
        Center(
          child: Container(
            width: 200,
            height: 200,
            decoration: BoxDecoration(shape: BoxShape.circle, border: Border.all(color: ringColor, width: 10)),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Ticker(
                  builder: (_, now) => Text(
                    formatClock(now.difference(trip.startedAtUtc)),
                    style: dmStyle(34, FontWeight.w800, color: c.text, tabular: true, spacing: -1.5),
                  ),
                ),
                Text('since ${Gst.time(trip.startedAtUtc)}', style: context.text.bodySmall),
                if (trip.isOverdue) ...[
                  const SizedBox(height: 6),
                  const StatusBadge('Overdue', tone: Tone.bad, icon: Icons.warning_rounded),
                ],
              ],
            ),
          ),
        ),
        if (trip.nextReminderAtUtc != null && !trip.isOverdue)
          Padding(
            padding: const EdgeInsets.only(top: 10),
            child: Text(
              'Next reminder at ${Gst.time(trip.nextReminderAtUtc!)}',
              textAlign: TextAlign.center,
              style: context.text.bodySmall,
            ),
          ),
        const SizedBox(height: 18),
        AppCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  PlateBadge(trip.currentVehicle.registrationNumber, large: true),
                  const SizedBox(width: 10),
                  Expanded(child: Text(trip.currentVehicle.type, style: context.text.titleSmall)),
                ],
              ),
              const SizedBox(height: 10),
              KeyValueRow('From', trip.originLot.name),
              KeyValueRow('Destination', trip.destination.label),
              const Divider(height: 24),
              SegmentTimeline(segments: trip.segments),
            ],
          ),
        ),
        const SizedBox(height: 16),
        FilledButton.icon(
          onPressed: () => context.push('/trip/end'),
          icon: const Icon(Icons.flag_rounded),
          label: const Text('End trip'),
        ),
        const SizedBox(height: 10),
        Row(
          children: [
            Expanded(
              child: OutlinedButton.icon(
                onPressed: () => context.push('/trip/swap'),
                icon: const Icon(Icons.sync_alt_rounded),
                label: const Text('Swap'),
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: OutlinedButton.icon(
                style: OutlinedButton.styleFrom(foregroundColor: c.dText),
                onPressed: () => context.push('/trip/cancel'),
                icon: const Icon(Icons.close_rounded),
                label: const Text('Cancel trip'),
              ),
            ),
          ],
        ),
      ],
    );
  }
}
