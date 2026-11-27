import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../state.dart';
import 'common.dart';
import 'scan.dart';

enum _SwapMode { vehicle, handOver }

/// S-14: change the vehicle (same trip, new segment) or hand the trip to another staff member (TRP-07, D-041).
class SwapScreen extends ConsumerStatefulWidget {
  const SwapScreen({super.key});

  @override
  ConsumerState<SwapScreen> createState() => _SwapScreenState();
}

class _SwapScreenState extends ConsumerState<SwapScreen> {
  _SwapMode? _mode;
  final _reason = TextEditingController();

  // Change vehicle
  ScanResult? _newVehicle;
  VehicleCard? _newVehicleCard;
  int? _oldVehicleLot = LotPlacePicker.notChosen;

  // Hand over
  final _search = TextEditingController();
  StaffRef? _toStaff;
  Future<List<StaffRef>>? _candidates;

  @override
  void dispose() {
    _reason.dispose();
    _search.dispose();
    super.dispose();
  }

  void _loadCandidates() =>
      setState(() => _candidates = ref.read(reposProvider).handoverCandidates(search: _search.text));

  Future<void> _scanNewVehicle() async {
    final result = await scanVehicle(context, purpose: 'Scan the new vehicle');
    if (result == null || !mounted) {
      return;
    }
    try {
      final lookup = await ref.read(reposProvider).lookupVehicle(qr: result.qr, registration: result.registration);
      setState(() {
        _newVehicle = result;
        _newVehicleCard = lookup.vehicle;
      });
    } on ApiException catch (error) {
      if (mounted) {
        showError(context, error);
      }
    }
  }

  Future<void> _submit(TripDetail trip) async {
    final repos = ref.read(reposProvider);
    try {
      if (_mode == _SwapMode.vehicle) {
        final updated = await repos.swapVehicle(
          trip.id,
          newVehicleQrToken: _newVehicle!.qr,
          newVehicleRegistration: _newVehicle!.registration,
          reason: _reason.text.trim(),
          oldVehicleLotId: _oldVehicleLot,
        );
        ref.read(activeTripProvider.notifier).set(updated);
        refreshAfterTripAction(ref);
        if (mounted) {
          showMessage(context, 'Vehicle changed. The trip continues.');
          context.pop();
        }
      } else {
        await repos.handOver(trip.id, toStaffId: _toStaff!.id, reason: _reason.text.trim());
        ref.read(activeTripProvider.notifier).set(null);
        refreshAfterTripAction(ref);
        if (mounted) {
          showMessage(context, 'Trip handed over to ${_toStaff!.fullName}.');
          context.go('/home');
        }
      }
    } on ApiException catch (error) {
      if (mounted) {
        showError(context, error);
      }
    }
  }

  bool get _ready =>
      _reason.text.trim().isNotEmpty &&
      (_mode == _SwapMode.vehicle
          ? _newVehicle != null && _oldVehicleLot != LotPlacePicker.notChosen
          : _toStaff != null);

  @override
  Widget build(BuildContext context) {
    final trip = ref.watch(activeTripProvider).valueOrNull;
    if (trip == null) {
      return const FlowScaffold(
        title: 'Swap',
        children: [EmptyView(icon: Icons.directions_car_rounded, title: 'No trip running', message: 'There is nothing to swap.')],
      );
    }
    return FlowScaffold(
      title: 'Swap',
      bottom: _mode == null
          ? null
          : BusyButton(
              label: _mode == _SwapMode.vehicle ? 'Confirm swap' : 'Hand over',
              icon: _mode == _SwapMode.vehicle ? Icons.sync_alt_rounded : Icons.group_rounded,
              onPressed: _ready ? () => _submit(trip) : null,
            ),
      children: [
        Text('What changes?', style: context.text.titleLarge),
        const SizedBox(height: 12),
        Row(
          children: [
            Expanded(
              child: _OptionCard(
                icon: Icons.directions_car_rounded,
                title: 'Change vehicle',
                subtitle: 'Trip continues',
                selected: _mode == _SwapMode.vehicle,
                onTap: () => setState(() => _mode = _SwapMode.vehicle),
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: _OptionCard(
                icon: Icons.group_rounded,
                title: 'Hand over',
                subtitle: 'To another staff',
                selected: _mode == _SwapMode.handOver,
                onTap: () {
                  setState(() => _mode = _SwapMode.handOver);
                  if (_candidates == null) {
                    _loadCandidates();
                  }
                },
              ),
            ),
          ],
        ),
        if (_mode == _SwapMode.vehicle) ..._vehicleFields(trip),
        if (_mode == _SwapMode.handOver) ..._handOverFields(),
        if (_mode != null) ...[
          const SectionLabel('Reason'),
          TextField(
            controller: _reason,
            maxLength: 500,
            minLines: 2,
            maxLines: 4,
            onChanged: (_) => setState(() {}),
            decoration: const InputDecoration(hintText: 'Why is it changing?'),
          ),
        ],
      ],
    );
  }

  List<Widget> _vehicleFields(TripDetail trip) => [
    const SectionLabel('New vehicle'),
    if (_newVehicleCard == null)
      OutlinedButton.icon(
        onPressed: _scanNewVehicle,
        icon: const Icon(Icons.qr_code_scanner_rounded),
        label: const Text('Scan the new vehicle'),
      )
    else ...[
      VehicleCardView(vehicle: _newVehicleCard!),
      TextButton(onPressed: _scanNewVehicle, child: const Text('Scan a different vehicle')),
    ],
    SectionLabel('Where is ${trip.currentVehicle.registrationNumber} left?'),
    LotPlacePicker(
      selected: _oldVehicleLot,
      preferredLotId: trip.originLot.id,
      onChanged: (value) => setState(() => _oldVehicleLot = value),
    ),
    const SizedBox(height: 12),
    Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(color: context.dm.iSoft, borderRadius: BorderRadius.circular(16)),
      child: Text('Same trip, new segment: your time keeps counting.', style: dmStyle(13.5, FontWeight.w600, color: context.dm.info)),
    ),
  ];

  List<Widget> _handOverFields() => [
    const SectionLabel('Hand over to'),
    TextField(
      controller: _search,
      textInputAction: TextInputAction.search,
      onSubmitted: (_) => _loadCandidates(),
      decoration: InputDecoration(
        hintText: 'Search name or Staff ID',
        prefixIcon: const Icon(Icons.search_rounded),
        suffixIcon: IconButton(onPressed: _loadCandidates, icon: const Icon(Icons.arrow_forward_rounded)),
      ),
    ),
    const SizedBox(height: 10),
    FutureBuilder<List<StaffRef>>(
      future: _candidates,
      builder: (context, snapshot) {
        if (snapshot.hasError) {
          return ErrorView(error: snapshot.error!, compact: true, onRetry: _loadCandidates);
        }
        if (!snapshot.hasData) {
          return const SkeletonList(count: 2, height: 60);
        }
        final people = snapshot.data!;
        if (people.isEmpty) {
          return const EmptyView(
            icon: Icons.person_off_rounded,
            title: 'Nobody available',
            message: 'Everyone else is on a trip or a transfer, or no one matches.',
          );
        }
        return Column(
          children: [
            for (final person in people) ...[
              AppCard(
                selected: _toStaff?.id == person.id,
                padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                onTap: () => setState(() => _toStaff = person),
                child: Row(
                  children: [
                    Avatar(name: person.fullName, photoUrl: person.photoUrl, size: 40),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(person.fullName, style: context.text.titleSmall),
                          Text(person.loginId, style: context.text.bodySmall),
                        ],
                      ),
                    ),
                    if (_toStaff?.id == person.id) Icon(Icons.check_circle_rounded, color: context.dm.primary),
                  ],
                ),
              ),
              const SizedBox(height: 8),
            ],
          ],
        );
      },
    ),
  ];
}

class _OptionCard extends StatelessWidget {
  const _OptionCard({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.selected,
    required this.onTap,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => AppCard(
    selected: selected,
    onTap: onTap,
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        IconTile(icon, size: 40),
        const SizedBox(height: 10),
        Text(title, style: context.text.titleSmall),
        Text(subtitle, style: context.text.bodySmall),
      ],
    ),
  );
}
