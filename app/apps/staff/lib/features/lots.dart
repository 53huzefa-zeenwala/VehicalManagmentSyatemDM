import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../state.dart';

/// S-18 Lots: free spaces per lot and per vehicle type (LOT-07). No map in v1 (D-056).
class LotsScreen extends ConsumerWidget {
  const LotsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final lots = ref.watch(lotsProvider);
    return RefreshIndicator(
      onRefresh: () async {
        ref.invalidate(lotsProvider);
        await ref.read(lotsProvider.future);
      },
      child: ListView(
        padding: const EdgeInsets.fromLTRB(DmSize.gutter, 12, DmSize.gutter, 24),
        children: [
          Text('Lots', style: context.text.headlineMedium),
          const SizedBox(height: 12),
          AsyncBody(
            value: lots,
            onRetry: () => ref.invalidate(lotsProvider),
            data: (items) => items.isEmpty
                ? const EmptyView(icon: Icons.local_parking_rounded, title: 'No lots', message: 'Ask an admin to add parking lots.')
                : Column(
                    children: [
                      for (final lot in items) ...[_LotCard(lot: lot), const SizedBox(height: 12)],
                    ],
                  ),
          ),
        ],
      ),
    );
  }
}

class _LotCard extends StatelessWidget {
  const _LotCard({required this.lot});

  final Lot lot;

  @override
  Widget build(BuildContext context) {
    final c = context.dm;
    final total = lot.total;
    final freeColor = total.isFull ? c.danger : (total.fill >= .85 ? c.warn : c.primary);
    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(lot.name, style: context.text.titleMedium),
                    Text('${lot.hours} · ${apiName(lot.lotType)}', style: context.text.bodySmall),
                  ],
                ),
              ),
              Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Text('${total.available}', style: dmStyle(26, FontWeight.w800, color: freeColor, tabular: true)),
                  Text('free of ${total.capacity}', style: context.text.labelSmall),
                ],
              ),
            ],
          ),
          const SizedBox(height: 10),
          CapacityBar(fill: total.fill, isFull: total.isFull),
          const SizedBox(height: 10),
          Wrap(
            spacing: 6,
            runSpacing: 6,
            children: [
              for (final type in lot.byType.where((t) => t.capacity != null))
                StatusBadge(
                  '${type.type} ${type.available}/${type.capacity}',
                  tone: (type.available ?? 1) <= 0 ? Tone.warn : Tone.neutral,
                ),
            ],
          ),
          const SizedBox(height: 6),
          Text(lot.address, style: context.text.bodySmall, maxLines: 2, overflow: TextOverflow.ellipsis),
        ],
      ),
    );
  }
}
