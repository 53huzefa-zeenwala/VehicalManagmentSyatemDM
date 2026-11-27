import 'dart:convert';

import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:signature/signature.dart';

import '../state.dart';
import 'common.dart';
import 'scan.dart';

class StartTripArgs {
  const StartTripArgs({required this.lookup, required this.scan});

  final VehicleLookup lookup;
  final ScanResult scan;
}

/// S-03 → S-05 → S-06 → S-07: confirm vehicle, pick destination, sign, review and start (TRP-01..03).
/// The origin is the lot the vehicle is parked at (D-047), so there is no lot step.
class StartTripScreen extends ConsumerStatefulWidget {
  const StartTripScreen({super.key, required this.args});

  final StartTripArgs args;

  @override
  ConsumerState<StartTripScreen> createState() => _StartTripScreenState();
}

class _StartTripScreenState extends ConsumerState<StartTripScreen> {
  int _step = 1;
  int? _areaId;
  Location? _location;
  Area? _area;
  final _signature = SignatureController(penStrokeWidth: 3, exportBackgroundColor: Colors.white);
  DateTime? _signedAt;

  VehicleCard get _vehicle => widget.args.lookup.vehicle;

  @override
  void initState() {
    super.initState();
    _signature.onDrawEnd = () => setState(() => _signedAt = DateTime.now().toUtc());
  }

  @override
  void dispose() {
    _signature.dispose();
    super.dispose();
  }

  void _back() {
    if (_step > 1) {
      setState(() => _step--);
    } else {
      context.pop();
    }
  }

  Future<void> _start() async {
    final png = await _signature.toPngBytes(height: 300, width: 600);
    if (png == null) {
      if (mounted) {
        setState(() => _step = 3);
      }
      return;
    }
    try {
      final trip = await ref.read(reposProvider).startTrip(
        vehicleQrToken: widget.args.scan.qr,
        vehicleRegistration: widget.args.scan.registration,
        destinationLocationId: _location!.id,
        signaturePngBase64: base64Encode(png),
      );
      ref.read(activeTripProvider.notifier).set(trip);
      refreshAfterTripAction(ref);
      if (mounted) {
        showMessage(context, 'Trip started. Drive safely.');
        context.go('/home');
      }
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      showError(context, error);
      if (error.status == 409 || error.status == 404) {
        // The vehicle or the staff member's state changed meanwhile: back to the vehicle step (UX §6 conflict).
        context.pop();
      }
    }
  }

  @override
  Widget build(BuildContext context) => PopScope(
    canPop: _step == 1,
    onPopInvokedWithResult: (didPop, _) {
      if (!didPop) {
        _back();
      }
    },
    child: switch (_step) {
      1 => _vehicleStep(),
      2 => _destinationStep(),
      3 => _signatureStep(),
      _ => _reviewStep(),
    },
  );

  Widget _vehicleStep() => FlowScaffold(
    title: 'Start trip',
    step: 1,
    totalSteps: 4,
    onBack: _back,
    bottom: FilledButton.icon(
      onPressed: () => setState(() => _step = 2),
      icon: const Icon(Icons.check_rounded),
      label: const Text('Confirm vehicle'),
    ),
    children: [
      Text('Is this the vehicle?', style: context.text.titleLarge),
      const SizedBox(height: 12),
      VehicleCardView(vehicle: _vehicle),
      const SizedBox(height: 8),
      TextButton.icon(
        onPressed: () => context.pop(),
        icon: const Icon(Icons.qr_code_scanner_rounded),
        label: const Text('Scan a different vehicle'),
      ),
    ],
  );

  Widget _destinationStep() {
    final areas = ref.watch(areasProvider);
    return FlowScaffold(
      title: 'Start trip',
      step: 2,
      totalSteps: 4,
      onBack: _back,
      bottom: FilledButton.icon(
        onPressed: _location == null ? null : () => setState(() => _step = 3),
        icon: const Icon(Icons.arrow_forward_rounded),
        label: const Text('Continue'),
      ),
      children: [
        Text('Where is it going?', style: context.text.titleLarge),
        const SizedBox(height: 12),
        AsyncBody(
          value: areas,
          onRetry: () => ref.invalidate(areasProvider),
          data: (items) {
            if (items.isEmpty) {
              return const EmptyView(
                icon: Icons.location_off_rounded,
                title: 'No destinations yet',
                message: 'Ask an admin to add areas and locations.',
              );
            }
            final area = items.firstWhere((a) => a.id == _areaId, orElse: () => items.first);
            return Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    for (final a in items)
                      ChoiceChip(
                        label: Text(a.name),
                        selected: a.id == area.id,
                        onSelected: (_) => setState(() => _areaId = a.id),
                      ),
                  ],
                ),
                const SizedBox(height: 14),
                for (final location in area.locations) ...[
                  AppCard(
                    selected: _location?.id == location.id,
                    padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
                    onTap: () => setState(() {
                      _location = location;
                      _area = area;
                    }),
                    child: Row(
                      children: [
                        const IconTile(Icons.location_on_rounded, size: 40),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(location.name, style: context.text.titleSmall),
                              Text(area.name, style: context.text.bodySmall),
                            ],
                          ),
                        ),
                        Icon(
                          _location?.id == location.id ? Icons.radio_button_checked_rounded : Icons.radio_button_unchecked_rounded,
                          color: context.dm.primary,
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 8),
                ],
              ],
            );
          },
        ),
      ],
    );
  }

  Widget _signatureStep() {
    final me = ref.watch(meProvider);
    final c = context.dm;
    return FlowScaffold(
      title: 'Start trip',
      step: 3,
      totalSteps: 4,
      onBack: _back,
      bottom: FilledButton.icon(
        onPressed: _signedAt == null ? null : () => setState(() => _step = 4),
        icon: const Icon(Icons.arrow_forward_rounded),
        label: const Text('Continue'),
      ),
      children: [
        Text('Sign to take the vehicle', style: context.text.titleLarge),
        const SizedBox(height: 12),
        AppCard(
          flat: true,
          child: Row(
            children: [
              Avatar(name: me.fullName, photoUrl: me.photoUrl),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(me.fullName, style: context.text.titleSmall),
                    Text('${me.loginId} · driver', style: context.text.bodySmall),
                  ],
                ),
              ),
              PlateBadge(_vehicle.registrationNumber),
            ],
          ),
        ),
        const SizedBox(height: 12),
        Container(
          height: 250,
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(DmSize.rCard),
            border: Border.all(color: c.border, width: 1.5),
          ),
          clipBehavior: Clip.antiAlias,
          child: Signature(controller: _signature, backgroundColor: Colors.white),
        ),
        const SizedBox(height: 8),
        Row(
          children: [
            Text(
              _signedAt == null ? 'Sign inside the box' : 'Signed ${Gst.time(_signedAt!)}',
              style: context.text.bodySmall,
            ),
            const Spacer(),
            TextButton.icon(
              onPressed: () => setState(() {
                _signature.clear();
                _signedAt = null;
              }),
              icon: const Icon(Icons.cleaning_services_rounded),
              label: const Text('Clear'),
            ),
          ],
        ),
      ],
    );
  }

  Widget _reviewStep() {
    final me = ref.watch(meProvider);
    return FlowScaffold(
      title: 'Start trip',
      step: 4,
      totalSteps: 4,
      onBack: _back,
      bottom: BusyButton(label: 'Start trip', icon: Icons.play_arrow_rounded, onPressed: _start),
      children: [
        Text('Check and start', style: context.text.titleLarge),
        const SizedBox(height: 12),
        AppCard(
          child: Column(
            children: [
              KeyValueRow('From', _vehicle.currentLot?.name ?? '—'),
              KeyValueRow('Vehicle', '', trailing: PlateBadge(_vehicle.registrationNumber)),
              KeyValueRow('Type', '${_vehicle.category} · ${_vehicle.type}'),
              KeyValueRow('Driver', 'You (${me.loginId})'),
              KeyValueRow(
                'Destination',
                '',
                trailing: InkWell(
                  onTap: () => setState(() => _step = 2),
                  child: Text('${_location!.name} · ${_area!.name}  ✎', style: context.text.titleSmall),
                ),
              ),
              KeyValueRow(
                'Signature',
                '',
                trailing: InkWell(
                  onTap: () => setState(() => _step = 3),
                  child: const StatusBadge('Signed', tone: Tone.ok, icon: Icons.verified_rounded),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        Text('The start time is recorded when you tap Start trip.', style: context.text.bodySmall),
      ],
    );
  }
}
