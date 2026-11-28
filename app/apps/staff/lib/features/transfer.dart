import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../state.dart';

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
          return ListView(
            padding: const EdgeInsets.fromLTRB(DmSize.gutter, 8, DmSize.gutter, 24),
            children: [
              WarningsBanner(warnings: t.warnings),
              TransferFactsCard(transfer: t),
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
