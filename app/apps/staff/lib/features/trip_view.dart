import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';

import 'common.dart';

/// Read-only trip card: vehicle, route, times, total, signature (S-13, S-17).
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
  Widget build(BuildContext context) {
    final c = context.dm;
    return Column(
      children: [
        for (final (index, segment) in segments.indexed)
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
                        color: segment.endedAtUtc == null ? c.accent : c.primary,
                        border: Border.all(color: (segment.endedAtUtc == null ? c.aSoft : c.pSoft), width: 4),
                      ),
                    ),
                    if (index < segments.length - 1) Expanded(child: Container(width: 2, color: c.border)),
                  ],
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Padding(
                    padding: const EdgeInsets.only(bottom: 14),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(_title(segment), style: context.text.titleSmall),
                        Text(
                          '${Gst.time(segment.startedAtUtc)} · ${segment.staff.fullName} · ${segment.vehicle.registrationNumber}',
                          style: context.text.bodySmall,
                        ),
                        if (segment.reason != null) Text('“${segment.reason}”', style: context.text.bodySmall),
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

  static String _title(TripSegment segment) => switch (segment.kind) {
    SegmentKind.start => 'Trip started',
    SegmentKind.vehicleSwap => 'Vehicle changed',
    SegmentKind.handOver => 'Handed over',
  };
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
