import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../state.dart';
import 'common.dart';
import 'trip_view.dart';

/// S-22: a transfer between lots. Any staff member confirms departure (and then drives, D-026/D-047) or arrival.
class TransferScreen extends ConsumerWidget {
  const TransferScreen({super.key, required this.id});

  final int id;

  Future<void> _act(BuildContext context, WidgetRef ref, Transfer transfer) async {
    final repos = ref.read(reposProvider);
    final departing = transfer.status == TransferStatus.pendingDeparture;
    try {
      final updated = departing ? await repos.departTransfer(id) : await repos.arriveTransfer(id);
      ref
        ..invalidate(transferProvider(id))
        ..invalidate(transferTasksProvider)
        ..invalidate(lotsProvider);
      if (context.mounted) {
        showMessage(
          context,
          departing ? 'Departure confirmed. You are driving this transfer.' : 'Arrival confirmed at ${updated.toLot.name}.',
        );
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
      appBar: AppBar(title: const Text('Transfer')),
      body: AsyncBody(
        value: transfer,
        onRetry: () => ref.invalidate(transferProvider(id)),
        data: (t) {
          final (label, tone) = switch (t.status) {
            TransferStatus.pendingDeparture => ('Ready to depart', Tone.info),
            TransferStatus.inTransit => ('On the way', Tone.live),
            TransferStatus.completed => ('Arrived', Tone.ok),
            TransferStatus.cancelled => ('Cancelled', Tone.bad),
          };
          return ListView(
            padding: const EdgeInsets.fromLTRB(DmSize.gutter, 8, DmSize.gutter, 24),
            children: [
              WarningsBanner(warnings: t.warnings),
              AppCard(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        PlateBadge(t.vehicle.registrationNumber, large: true),
                        const Spacer(),
                        StatusBadge(label, tone: tone),
                      ],
                    ),
                    const SizedBox(height: 12),
                    Text('${t.fromLot.name}  →  ${t.toLot.name}', style: context.text.titleMedium),
                    const SizedBox(height: 8),
                    KeyValueRow('Type', '${t.vehicle.category} · ${t.vehicle.type}'),
                    KeyValueRow('Created', '${Gst.dateTime(t.createdAtUtc)} by ${t.createdBy.fullName}'),
                    if (t.note != null && t.note!.isNotEmpty) KeyValueRow('Note', t.note!),
                    if (t.driver != null) KeyValueRow('Driver', t.driver!.fullName),
                    if (t.departedAtUtc != null) KeyValueRow('Departed', Gst.dateTime(t.departedAtUtc!)),
                    if (t.arrivedAtUtc != null) KeyValueRow('Arrived', Gst.dateTime(t.arrivedAtUtc!)),
                    if (t.status == TransferStatus.inTransit && t.departedAtUtc != null)
                      Ticker(builder: (_, now) => KeyValueRow('Driving', formatClock(now.difference(t.departedAtUtc!)))),
                    if (t.status == TransferStatus.completed && t.drivingSeconds != null)
                      KeyValueRow('Driving time', formatDuration(Duration(seconds: t.drivingSeconds!))),
                  ],
                ),
              ),
              const SizedBox(height: 16),
              if (t.status == TransferStatus.pendingDeparture) ...[
                Text(
                  'Whoever confirms departure drives the vehicle. You can’t be on a trip at the same time.',
                  style: context.text.bodySmall,
                ),
                const SizedBox(height: 10),
                BusyButton(label: 'Confirm departure', icon: Icons.local_shipping_rounded, onPressed: () => _act(context, ref, t)),
              ],
              if (t.status == TransferStatus.inTransit)
                BusyButton(label: 'Confirm arrival', icon: Icons.flag_rounded, onPressed: () => _act(context, ref, t)),
            ],
          );
        },
      ),
    );
  }
}
