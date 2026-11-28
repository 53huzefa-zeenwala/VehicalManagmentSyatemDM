import 'package:flutter/material.dart';

import '../models/models.dart';
import '../theme/app_theme.dart';
import '../theme/tokens.dart';
import '../time/gst.dart';
import 'flow.dart';
import 'widgets.dart';

/// Read-only trip card: vehicle, route, times, total, signature (S-13, S-17, A-04).
class TripFactsCard extends StatelessWidget {
  const TripFactsCard({super.key, required this.trip});

  final TripDetail trip;

  @override
  Widget build(BuildContext context) {
    final vehicle = trip.currentVehicle;
    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              PlateBadge(vehicle.registrationNumber, large: true),
              const Spacer(),
              tripStatusBadge(trip.status, overdue: trip.isOverdue),
            ],
          ),
          const SizedBox(height: 10),
          KeyValueRow('Type', '${vehicle.category} · ${vehicle.type}'),
          KeyValueRow('Brand · model', '${vehicle.brand} ${vehicle.model}'),
          KeyValueRow('From', trip.originLot.name),
          KeyValueRow('Destination', trip.destination.label),
          if (trip.returnLot != null) KeyValueRow('Returned to', trip.returnLot!.name),
          KeyValueRow('Start', Gst.dateTime(trip.startedAtUtc)),
          if (trip.endedAtUtc != null) KeyValueRow('End', Gst.dateTime(trip.endedAtUtc!)),
          if (trip.totalSeconds != null) KeyValueRow('Total', formatDuration(Duration(seconds: trip.totalSeconds!))),
          if (trip.isActive)
            Ticker(builder: (_, now) => KeyValueRow('Running for', formatClock(now.difference(trip.startedAtUtc)))),
          KeyValueRow('Driver (staff)', trip.currentStaff.fullName),
          if (trip.cancel != null) ...[
            KeyValueRow('Cancel reason', apiName(trip.cancel!.category)),
            KeyValueRow('Note', trip.cancel!.note),
          ],
          if (trip.signatureUrl != null) ...[
            const SizedBox(height: 8),
            Text('Signature', style: context.text.bodySmall),
            const SizedBox(height: 6),
            Container(
              height: 90,
              width: double.infinity,
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(14),
                border: Border.all(color: context.dm.border),
              ),
              child: AuthImage(trip.signatureUrl!, fit: BoxFit.contain),
            ),
          ],
        ],
      ),
    );
  }
}

/// Segment timeline: who drove which vehicle when (start, vehicle swaps, hand-overs).
class SegmentTimeline extends StatelessWidget {
  const SegmentTimeline({super.key, required this.segments});

  final List<TripSegment> segments;

  @override
  Widget build(BuildContext context) => TimelineList(
    items: [
      for (final segment in segments)
        TimelineItem(
          title: _title(segment),
          lines: [
            '${Gst.time(segment.startedAtUtc)} · ${segment.staff.fullName} · ${segment.vehicle.registrationNumber}',
            if (segment.reason != null) '“${segment.reason}”',
          ],
          live: segment.endedAtUtc == null,
        ),
    ],
  );

  static String _title(TripSegment segment) => switch (segment.kind) {
    SegmentKind.start => 'Trip started',
    SegmentKind.vehicleSwap => 'Vehicle changed',
    SegmentKind.handOver => 'Handed over',
  };
}

class TimelineItem {
  const TimelineItem({required this.title, this.lines = const [], this.live = false});

  final String title;
  final List<String> lines;

  /// Still running (accent dot) rather than done (primary dot).
  final bool live;
}

/// Vertical timeline with dots and a connecting line.
class TimelineList extends StatelessWidget {
  const TimelineList({super.key, required this.items});

  final List<TimelineItem> items;

  @override
  Widget build(BuildContext context) {
    final c = context.dm;
    return Column(
      children: [
        for (final (index, item) in items.indexed)
          IntrinsicHeight(
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Column(
                  children: [
                    Container(
                      margin: const EdgeInsets.only(top: 3),
                      width: 18,
                      height: 18,
                      decoration: BoxDecoration(
                        shape: BoxShape.circle,
                        color: item.live ? c.accent : c.primary,
                        border: Border.all(color: item.live ? c.aSoft : c.pSoft, width: 4),
                      ),
                    ),
                    if (index < items.length - 1) Expanded(child: Container(width: 2, color: c.border)),
                  ],
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Padding(
                    padding: const EdgeInsets.only(bottom: 14),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(item.title, style: context.text.titleSmall),
                        for (final line in item.lines) Text(line, style: context.text.bodySmall),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ),
      ],
    );
  }
}

/// Transfer facts and timeline (S-22, A-07): created → departed (driver) → arrived.
class TransferFactsCard extends StatelessWidget {
  const TransferFactsCard({super.key, required this.transfer});

  final Transfer transfer;

  @override
  Widget build(BuildContext context) {
    final t = transfer;
    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              PlateBadge(t.vehicle.registrationNumber, large: true),
              const Spacer(),
              transferStatusBadge(t.status, overdue: t.isOverdue),
            ],
          ),
          const SizedBox(height: 12),
          Text('${t.fromLot.name}  →  ${t.toLot.name}', style: context.text.titleMedium),
          const SizedBox(height: 8),
          KeyValueRow('Type', '${t.vehicle.category} · ${t.vehicle.type}'),
          if (t.note != null && t.note!.isNotEmpty) KeyValueRow('Note', t.note!),
          if (t.status == TransferStatus.inTransit && t.departedAtUtc != null)
            Ticker(builder: (_, now) => KeyValueRow('Driving', formatClock(now.difference(t.departedAtUtc!)))),
          if (t.status == TransferStatus.completed && t.drivingSeconds != null)
            KeyValueRow('Driving time', formatDuration(Duration(seconds: t.drivingSeconds!))),
          const SizedBox(height: 12),
          TimelineList(
            items: [
              TimelineItem(title: 'Created', lines: ['${Gst.dateTime(t.createdAtUtc)} · ${t.createdBy.fullName}']),
              if (t.departedAtUtc != null)
                TimelineItem(
                  title: 'Departed',
                  lines: ['${Gst.dateTime(t.departedAtUtc!)} · driver ${t.driver?.fullName ?? '—'}'],
                  live: t.status == TransferStatus.inTransit,
                ),
              if (t.arrivedAtUtc != null)
                TimelineItem(
                  title: 'Arrived',
                  lines: ['${Gst.dateTime(t.arrivedAtUtc!)} · confirmed by ${t.arrivedBy?.fullName ?? '—'}'],
                ),
              if (t.status == TransferStatus.cancelled) const TimelineItem(title: 'Cancelled'),
            ],
          ),
        ],
      ),
    );
  }
}

/// Non-blocking warnings from the API (e.g. LOT_FULL), shown after an action.
class WarningsBanner extends StatelessWidget {
  const WarningsBanner({super.key, required this.warnings});

  final List<Notice> warnings;

  @override
  Widget build(BuildContext context) {
    if (warnings.isEmpty) {
      return const SizedBox.shrink();
    }
    final c = context.dm;
    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(color: c.wSoft, borderRadius: BorderRadius.circular(16)),
      child: Column(
        children: [
          for (final warning in warnings)
            Row(
              children: [
                Icon(Icons.warning_rounded, color: c.warn),
                const SizedBox(width: 10),
                Expanded(child: Text(warning.message, style: dmStyle(13.5, FontWeight.w600, color: c.warn))),
              ],
            ),
        ],
      ),
    );
  }
}
