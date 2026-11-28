import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../state.dart';

/// Vehicle card: illustration, plate, status, details (S-03).
class VehicleCardView extends StatelessWidget {
  const VehicleCardView({super.key, required this.vehicle, this.badge});

  final VehicleCard vehicle;
  final Widget? badge;

  @override
  Widget build(BuildContext context) => AppCard(
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Center(
          child: vehicle.photoUrl != null
              ? ClipRRect(
                  borderRadius: BorderRadius.circular(18),
                  child: AuthImage(vehicle.photoUrl!, height: 130, width: double.infinity),
                )
              : Illustration(vehicle.category.toLowerCase().contains('car') ? 'car' : 'truck', height: 100),
        ),
        const SizedBox(height: 12),
        Row(
          children: [
            PlateBadge(vehicle.registrationNumber, large: true),
            const Spacer(),
            badge ?? vehicleStatusBadge(vehicle.status),
          ],
        ),
        const SizedBox(height: 10),
        KeyValueRow('Type', '${vehicle.category} · ${vehicle.type}'),
        KeyValueRow('Brand · model', '${vehicle.brand} ${vehicle.model}'),
        KeyValueRow('Colour', vehicle.colour),
        KeyValueRow('Parked at', vehicle.currentLot?.name ?? 'Not at a lot'),
      ],
    ),
  );
}

/// Where a vehicle is left: one of the active lots, or "Not at a lot" (null).
class LotPlacePicker extends ConsumerWidget {
  const LotPlacePicker({super.key, required this.selected, required this.onChanged, this.preferredLotId});

  /// Null = "Not at a lot"; use [notChosen] before the user picks.
  final int? selected;
  final ValueChanged<int?> onChanged;
  final int? preferredLotId;

  static const int notChosen = -1;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final lots = ref.watch(lotsProvider);
    return AsyncBody(
      value: lots,
      onRetry: () => ref.invalidate(lotsProvider),
      loading: const SkeletonList(count: 2, height: 64),
      data: (items) {
        final sorted = [...items]..sort((a, b) => a.id == preferredLotId ? -1 : (b.id == preferredLotId ? 1 : 0));
        return Column(
          children: [
            for (final lot in sorted) ...[
              _PlaceTile(
                title: lot.name,
                subtitle: '${lot.total.available} free of ${lot.total.capacity}',
                selected: selected == lot.id,
                warning: lot.total.isFull ? 'Full' : null,
                onTap: () => onChanged(lot.id),
              ),
              const SizedBox(height: 8),
            ],
            _PlaceTile(
              title: 'Not at a lot',
              subtitle: 'The vehicle is left somewhere else; an admin resolves it',
              selected: selected == null,
              onTap: () => onChanged(null),
            ),
          ],
        );
      },
    );
  }
}

class _PlaceTile extends StatelessWidget {
  const _PlaceTile({required this.title, required this.subtitle, required this.selected, required this.onTap, this.warning});

  final String title;
  final String subtitle;
  final bool selected;
  final String? warning;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => AppCard(
    selected: selected,
    onTap: onTap,
    padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
    child: Row(
      children: [
        Icon(selected ? Icons.radio_button_checked_rounded : Icons.radio_button_unchecked_rounded, color: context.dm.primary),
        const SizedBox(width: 12),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(title, style: context.text.titleSmall),
              Text(subtitle, style: context.text.bodySmall),
            ],
          ),
        ),
        if (warning != null) StatusBadge(warning!, tone: Tone.warn, icon: Icons.warning_rounded),
      ],
    ),
  );
}

/// Refreshes what the trip screens and Home show after a trip action.
void refreshAfterTripAction(WidgetRef ref) {
  ref
    ..invalidate(lotsProvider)
    ..invalidate(transferTasksProvider)
    ..invalidate(tripsProvider);
  ref.read(sessionProvider.notifier).refreshMe().ignore();
}
