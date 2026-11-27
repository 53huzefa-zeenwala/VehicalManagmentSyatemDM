import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../state.dart';

/// Screen for a step-by-step flow: back/close, title, optional stepper, scrollable body, action pinned at the bottom.
class FlowScaffold extends StatelessWidget {
  const FlowScaffold({
    super.key,
    required this.title,
    required this.children,
    this.step,
    this.totalSteps,
    this.bottom,
    this.onBack,
  });

  final String title;
  final int? step;
  final int? totalSteps;
  final List<Widget> children;
  final Widget? bottom;
  final VoidCallback? onBack;

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      leading: IconButton(
        icon: const Icon(Icons.arrow_back_rounded),
        onPressed: onBack ?? () => Navigator.of(context).maybePop(),
      ),
      title: Text(step == null ? title : '$title · Step $step of $totalSteps'),
    ),
    body: SafeArea(
      child: Column(
        children: [
          if (step != null)
            Padding(
              padding: const EdgeInsets.fromLTRB(DmSize.gutter, 4, DmSize.gutter, 8),
              child: StepperBar(step: step!, total: totalSteps!),
            ),
          Expanded(
            child: ListView(
              padding: const EdgeInsets.fromLTRB(DmSize.gutter, 8, DmSize.gutter, DmSize.gutter),
              children: children,
            ),
          ),
          if (bottom != null)
            Padding(padding: const EdgeInsets.fromLTRB(DmSize.gutter, 8, DmSize.gutter, 16), child: bottom),
        ],
      ),
    ),
  );
}

/// Primary action that shows progress while [onPressed] runs and can't be double-tapped.
class BusyButton extends StatefulWidget {
  const BusyButton({super.key, required this.label, required this.onPressed, this.icon, this.danger = false});

  final String label;
  final IconData? icon;
  final bool danger;

  /// Null disables the button.
  final Future<void> Function()? onPressed;

  @override
  State<BusyButton> createState() => _BusyButtonState();
}

class _BusyButtonState extends State<BusyButton> {
  bool _busy = false;

  @override
  Widget build(BuildContext context) {
    final c = context.dm;
    final style = widget.danger ? FilledButton.styleFrom(backgroundColor: c.danger, foregroundColor: Colors.white) : null;
    final onPressed = widget.onPressed == null || _busy
        ? null
        : () async {
            setState(() => _busy = true);
            try {
              await widget.onPressed!();
            } finally {
              if (mounted) {
                setState(() => _busy = false);
              }
            }
          };
    return FilledButton.icon(
      style: style,
      onPressed: onPressed,
      icon: _busy
          ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2.4))
          : Icon(widget.icon ?? Icons.check_rounded),
      label: Text(widget.label),
    );
  }
}

class SectionLabel extends StatelessWidget {
  const SectionLabel(this.text, {super.key});

  final String text;

  @override
  Widget build(BuildContext context) =>
      Padding(padding: const EdgeInsets.only(top: 18, bottom: 8), child: Text(text, style: context.text.labelMedium));
}

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

Widget vehicleStatusBadge(VehicleStatus status) => switch (status) {
  VehicleStatus.available => const StatusBadge('Available', tone: Tone.ok, icon: Icons.check_circle_rounded),
  VehicleStatus.active => const StatusBadge('On a trip', tone: Tone.live, icon: Icons.radio_button_checked_rounded),
  VehicleStatus.inactive => const StatusBadge('Inactive', tone: Tone.neutral, icon: Icons.block_rounded),
  VehicleStatus.underMaintenance => const StatusBadge('Maintenance', tone: Tone.warn, icon: Icons.build_rounded),
};

Widget tripStatusBadge(TripStatus status, {bool overdue = false}) => switch (status) {
  TripStatus.active when overdue => const StatusBadge('Overdue', tone: Tone.bad, icon: Icons.warning_rounded),
  TripStatus.active => const StatusBadge('Live', tone: Tone.live, icon: Icons.radio_button_checked_rounded),
  TripStatus.ended => const StatusBadge('Completed', tone: Tone.ok, icon: Icons.task_alt_rounded),
  TripStatus.cancelled => const StatusBadge('Cancelled', tone: Tone.bad, icon: Icons.cancel_rounded),
};

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
