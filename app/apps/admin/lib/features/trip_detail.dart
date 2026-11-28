import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../state.dart';

/// A-04: a trip, read-only. Admins monitor; only staff start, change or end trips (D-021).
class TripDetailScreen extends ConsumerWidget {
  const TripDetailScreen({super.key, required this.id});

  final int id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final trip = ref.watch(tripProvider(id));
    return Scaffold(
      appBar: AppBar(title: Text('Trip #$id')),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(tripProvider(id));
          await ref.read(tripProvider(id).future);
        },
        child: AsyncBody(
          value: trip,
          onRetry: () => ref.invalidate(tripProvider(id)),
          data: (t) => ListView(
            padding: const EdgeInsets.fromLTRB(DmSize.gutter, 8, DmSize.gutter, 24),
            children: [
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                decoration: BoxDecoration(color: context.dm.iSoft, borderRadius: BorderRadius.circular(14)),
                child: Row(
                  children: [
                    Icon(Icons.visibility_rounded, color: context.dm.info, size: 20),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        'Monitoring only. The driver ends, swaps or cancels the trip in the staff app.',
                        style: dmStyle(13, FontWeight.w600, color: context.dm.info),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 12),
              TripFactsCard(trip: t),
              if (t.isActive && t.nextReminderAtUtc != null) ...[
                const SizedBox(height: 8),
                Text(
                  t.isOverdue
                      ? 'Overdue: past the reminder time. Next reminder ${Gst.time(t.nextReminderAtUtc!)}.'
                      : 'First reminder at ${Gst.time(t.nextReminderAtUtc!)}.',
                  style: context.text.bodySmall,
                ),
              ],
              const SectionLabel('Timeline'),
              AppCard(child: SegmentTimeline(segments: t.segments)),
              const SectionLabel('Reminders sent'),
              AppCard(
                child: t.reminders.isEmpty
                    ? Text('None yet.', style: context.text.bodyMedium)
                    : Column(
                        children: [
                          for (final reminder in t.reminders)
                            KeyValueRow('Reminder ${reminder.number}', Gst.dateTime(reminder.sentAtUtc)),
                        ],
                      ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
