import 'dart:async';

import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../state.dart';
import 'common.dart';
import 'start_trip.dart';

/// What the scanner returns: the QR token from the sticker, or a registration number typed by hand.
class ScanResult {
  const ScanResult.qr(String value) : qr = value, registration = null;

  const ScanResult.registration(String value) : registration = value, qr = null;

  final String? qr;
  final String? registration;

  String get display => qr ?? registration!;
}

/// Opens the scanner and returns the result (null if closed).
Future<ScanResult?> scanVehicle(BuildContext context, {String purpose = 'Scan vehicle'}) =>
    context.push<ScanResult>('/scan', extra: purpose);

/// What the centre Scan button does, by context (UX_ARCHITECTURE.md C-09).
abstract final class ScanFlow {
  static Future<void> fromTabBar(BuildContext context, WidgetRef ref) async {
    final active = ref.read(activeTripProvider).valueOrNull;
    if (active != null) {
      await context.push('/trip/end');
      return;
    }

    final result = await scanVehicle(context, purpose: 'Scan to start a trip');
    if (result == null || !context.mounted) {
      return;
    }
    await openScanned(context, ref, result);
  }

  /// Looks the vehicle up and goes to the right place: start a trip, a transfer task, or explain the block.
  static Future<void> openScanned(BuildContext context, WidgetRef ref, ScanResult result) async {
    final repos = ref.read(reposProvider);
    try {
      final lookup = await repos.lookupVehicle(qr: result.qr, registration: result.registration);
      if (!context.mounted) {
        return;
      }
      if (lookup.canStart) {
        await context.push('/start', extra: StartTripArgs(lookup: lookup, scan: result));
        return;
      }

      final reason = lookup.blockReason;
      if (reason?.code == 'VEHICLE_IN_TRANSFER' && result.qr != null) {
        final transfer = await repos.transferByVehicle(result.qr!);
        if (context.mounted) {
          await context.push('/transfers/${transfer.id}');
        }
        return;
      }
      if (reason?.code == 'STAFF_HAS_ACTIVE_TRIP') {
        await ref.read(activeTripProvider.notifier).refresh();
        if (context.mounted) {
          await context.push('/trip');
        }
        return;
      }
      await showModalBottomSheet<void>(
        context: context,
        isScrollControlled: true,
        builder: (_) => _BlockedSheet(lookup: lookup),
      );
    } on ApiException catch (error) {
      if (context.mounted) {
        showError(context, error);
      }
    }
  }
}

class _BlockedSheet extends StatelessWidget {
  const _BlockedSheet({required this.lookup});

  final VehicleLookup lookup;

  @override
  Widget build(BuildContext context) => SafeArea(
    child: SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(DmSize.gutter, 0, DmSize.gutter, 24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text("Can't start with this vehicle", style: context.text.titleLarge),
          const SizedBox(height: 12),
          Container(
            padding: const EdgeInsets.all(14),
            decoration: BoxDecoration(color: context.dm.dSoft, borderRadius: BorderRadius.circular(16)),
            child: Row(
              children: [
                Icon(Icons.block_rounded, color: context.dm.dText),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(
                    lookup.blockReason?.message ?? 'This vehicle is not available.',
                    style: dmStyle(14, FontWeight.w600, color: context.dm.dText),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 12),
          VehicleCardView(vehicle: lookup.vehicle),
          const SizedBox(height: 16),
          FilledButton(onPressed: () => Navigator.of(context).pop(), child: const Text('OK')),
        ],
      ),
    ),
  );
}

/// C-09: camera viewfinder for the QR sticker, torch, and "Type registration number" instead.
class ScannerScreen extends StatefulWidget {
  const ScannerScreen({super.key, required this.purpose});

  final String purpose;

  @override
  State<ScannerScreen> createState() => _ScannerScreenState();
}

class _ScannerScreenState extends State<ScannerScreen> {
  final _controller = MobileScannerController(formats: const [BarcodeFormat.qrCode]);
  bool _done = false;

  /// Set when the camera still isn't running after a few seconds (e.g. access denied earlier and Android no longer
  /// asks), so the screen explains it instead of staying black.
  bool _cameraLate = false;
  Timer? _cameraCheck;

  @override
  void initState() {
    super.initState();
    _cameraCheck = Timer(const Duration(seconds: 4), () {
      if (mounted && !_controller.value.isRunning) {
        setState(() => _cameraLate = true);
      }
    });
  }

  @override
  void dispose() {
    _cameraCheck?.cancel();
    _controller.dispose();
    super.dispose();
  }

  void _onDetect(BarcodeCapture capture) {
    final value = capture.barcodes.map((b) => b.rawValue).whereType<String>().firstOrNull?.trim();
    if (_done || value == null || value.isEmpty) {
      return;
    }
    _done = true;
    context.pop(ScanResult.qr(value));
  }

  Future<void> _typeRegistration() async {
    final value = await showDialog<String>(context: context, builder: (_) => const _RegistrationDialog());
    if (value != null && value.trim().isNotEmpty && mounted) {
      _done = true;
      context.pop(ScanResult.registration(value.trim()));
    }
  }

  @override
  Widget build(BuildContext context) {
    final c = context.dm;
    return Scaffold(
      backgroundColor: const Color(0xFF0B1112),
      body: Stack(
        fit: StackFit.expand,
        children: [
          MobileScanner(
            controller: _controller,
            onDetect: _onDetect,
            // The message for camera problems is drawn above the viewfinder (below), so it is never hidden.
            errorBuilder: (context, error) => const SizedBox.shrink(),
          ),
          ValueListenableBuilder<MobileScannerState>(
            valueListenable: _controller,
            builder: (context, state, _) {
              final error = state.error;
              if (error == null && (state.isRunning || !_cameraLate)) {
                return const SizedBox.shrink();
              }
              return Align(
                alignment: const Alignment(0, -0.62),
                child: Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 32),
                  child: Text(
                    error == null || error.errorCode == MobileScannerErrorCode.permissionDenied
                        ? 'Camera access is off. Allow it for DM Staff in Settings, or type the registration number below.'
                        : 'The camera could not start. Type the registration number below.',
                    textAlign: TextAlign.center,
                    style: dmStyle(15, FontWeight.w600, color: Colors.white),
                  ),
                ),
              );
            },
          ),
          Center(
            child: Container(
              width: 250,
              height: 250,
              decoration: BoxDecoration(
                border: Border.all(color: c.primary, width: 3),
                borderRadius: BorderRadius.circular(28),
              ),
            ),
          ),
          Align(
            alignment: Alignment.topCenter,
            child: SafeArea(
              child: Padding(
                padding: const EdgeInsets.all(12),
                child: Row(
                  children: [
                    _GlassButton(icon: Icons.close_rounded, tooltip: 'Close', onPressed: () => context.pop()),
                    Expanded(
                      child: Text(
                        widget.purpose,
                        textAlign: TextAlign.center,
                        style: dmStyle(16, FontWeight.w700, color: Colors.white),
                      ),
                    ),
                    _GlassButton(icon: Icons.flashlight_on_rounded, tooltip: 'Torch', onPressed: () => _controller.toggleTorch()),
                  ],
                ),
              ),
            ),
          ),
          Align(
            alignment: Alignment.bottomCenter,
            child: SafeArea(
              child: Container(
                margin: const EdgeInsets.all(DmSize.gutter),
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.white.withValues(alpha: .12),
                  borderRadius: BorderRadius.circular(22),
                  border: Border.all(color: Colors.white.withValues(alpha: .2)),
                ),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text('Point at the QR sticker', style: dmStyle(16, FontWeight.w700, color: Colors.white)),
                    const SizedBox(height: 12),
                    OutlinedButton.icon(
                      onPressed: _typeRegistration,
                      icon: const Icon(Icons.keyboard_rounded),
                      label: const Text('Type registration number'),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _GlassButton extends StatelessWidget {
  const _GlassButton({required this.icon, required this.tooltip, required this.onPressed});

  final IconData icon;
  final String tooltip;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) => IconButton(
    tooltip: tooltip,
    onPressed: onPressed,
    style: IconButton.styleFrom(
      backgroundColor: Colors.white.withValues(alpha: .16),
      foregroundColor: Colors.white,
      fixedSize: const Size(46, 46),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
    ),
    icon: Icon(icon),
  );
}

class _RegistrationDialog extends StatefulWidget {
  const _RegistrationDialog();

  @override
  State<_RegistrationDialog> createState() => _RegistrationDialogState();
}

class _RegistrationDialogState extends State<_RegistrationDialog> {
  final _text = TextEditingController();

  @override
  void dispose() {
    _text.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AlertDialog(
    title: const Text('Registration number'),
    content: TextField(
      controller: _text,
      autofocus: true,
      textCapitalization: TextCapitalization.characters,
      maxLength: 30,
      decoration: const InputDecoration(hintText: 'e.g. DXB-A-12345', counterText: ''),
      onSubmitted: (value) => Navigator.of(context).pop(value),
    ),
    actions: [
      TextButton(onPressed: () => Navigator.of(context).pop(), child: const Text('Cancel')),
      TextButton(onPressed: () => Navigator.of(context).pop(_text.text), child: const Text('Continue')),
    ],
  );
}
