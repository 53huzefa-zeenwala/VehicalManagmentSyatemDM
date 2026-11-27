import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../state.dart';
import 'common.dart';

/// S-15: reason category, required note (D-046), where the vehicle is now; hours still count (TRP-08).
class CancelTripScreen extends ConsumerStatefulWidget {
  const CancelTripScreen({super.key});

  @override
  ConsumerState<CancelTripScreen> createState() => _CancelTripScreenState();
}

class _CancelTripScreenState extends ConsumerState<CancelTripScreen> {
  CancelCategory? _category;
  final _note = TextEditingController();
  int? _lot = LotPlacePicker.notChosen;

  @override
  void dispose() {
    _note.dispose();
    super.dispose();
  }

  Future<void> _submit(TripDetail trip) async {
    final sure = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Cancel this trip?'),
        content: const Text('The trip stops now. The time so far still counts in your hours.'),
        actions: [
          TextButton(onPressed: () => Navigator.of(context).pop(false), child: const Text('Keep trip')),
          TextButton(onPressed: () => Navigator.of(context).pop(true), child: const Text('Cancel trip')),
        ],
      ),
    );
    if (sure != true || !mounted) {
      return;
    }
    try {
      await ref.read(reposProvider).cancelTrip(trip.id, category: _category!, note: _note.text.trim(), vehicleLotId: _lot);
      ref.read(activeTripProvider.notifier).set(null);
      refreshAfterTripAction(ref);
      if (mounted) {
        showMessage(context, 'Trip cancelled.');
        context.go('/home');
      }
    } on ApiException catch (error) {
      if (mounted) {
        showError(context, error);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final trip = ref.watch(activeTripProvider).valueOrNull;
    if (trip == null) {
      return const FlowScaffold(
        title: 'Cancel trip',
        children: [EmptyView(icon: Icons.directions_car_rounded, title: 'No trip running', message: 'There is nothing to cancel.')],
      );
    }
    final ready = _category != null && _note.text.trim().isNotEmpty && _lot != LotPlacePicker.notChosen;
    const reasons = {
      CancelCategory.driver: (Icons.person_off_rounded, 'Driver'),
      CancelCategory.vehicle: (Icons.car_crash_rounded, 'Vehicle'),
      CancelCategory.technical: (Icons.build_rounded, 'Technical'),
      CancelCategory.other: (Icons.more_horiz_rounded, 'Other'),
    };
    return FlowScaffold(
      title: 'Cancel trip',
      bottom: BusyButton(
        label: 'Cancel trip',
        icon: Icons.close_rounded,
        danger: true,
        onPressed: ready ? () => _submit(trip) : null,
      ),
      children: [
        Text('Why is the trip cancelled?', style: context.text.titleLarge),
        const SizedBox(height: 12),
        GridView.count(
          crossAxisCount: 2,
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          mainAxisSpacing: 10,
          crossAxisSpacing: 10,
          childAspectRatio: 2.2,
          children: [
            for (final entry in reasons.entries)
              AppCard(
                selected: _category == entry.key,
                onTap: () => setState(() => _category = entry.key),
                padding: const EdgeInsets.all(12),
                child: Row(
                  children: [
                    IconTile(entry.value.$1, tone: Tone.bad, size: 38),
                    const SizedBox(width: 10),
                    Text(entry.value.$2, style: context.text.titleSmall),
                  ],
                ),
              ),
          ],
        ),
        const SectionLabel('Note (required)'),
        TextField(
          controller: _note,
          maxLength: 500,
          minLines: 2,
          maxLines: 4,
          onChanged: (_) => setState(() {}),
          decoration: const InputDecoration(hintText: 'What happened?'),
        ),
        SectionLabel('Where is ${trip.currentVehicle.registrationNumber} now?'),
        LotPlacePicker(
          selected: _lot,
          preferredLotId: trip.originLot.id,
          onChanged: (value) => setState(() => _lot = value),
        ),
        const SizedBox(height: 12),
        Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(color: context.dm.aSoft, borderRadius: BorderRadius.circular(16)),
          child: Text(
            'Hours still count: the time until now stays in your total.',
            style: dmStyle(13.5, FontWeight.w600, color: context.dm.aText),
          ),
        ),
      ],
    );
  }
}
