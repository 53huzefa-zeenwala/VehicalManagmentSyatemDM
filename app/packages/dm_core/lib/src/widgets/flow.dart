import 'package:flutter/material.dart';

import '../models/models.dart';
import '../theme/tokens.dart';
import 'widgets.dart';

/// Screen for a form or a step-by-step flow: back, title, optional stepper, scrollable body, action pinned at the
/// bottom.
class FlowScaffold extends StatelessWidget {
  const FlowScaffold({
    super.key,
    required this.title,
    required this.children,
    this.step,
    this.totalSteps,
    this.bottom,
    this.onBack,
    this.actions,
  });

  final String title;
  final int? step;
  final int? totalSteps;
  final List<Widget> children;
  final Widget? bottom;
  final VoidCallback? onBack;
  final List<Widget>? actions;

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      leading: IconButton(
        icon: const Icon(Icons.arrow_back_rounded),
        onPressed: onBack ?? () => Navigator.of(context).maybePop(),
      ),
      title: Text(step == null ? title : '$title · Step $step of $totalSteps'),
      actions: actions,
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
  const BusyButton({
    super.key,
    required this.label,
    required this.onPressed,
    this.icon,
    this.danger = false,
    this.outlined = false,
  });

  final String label;
  final IconData? icon;
  final bool danger;
  final bool outlined;

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
    final icon = _busy
        ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2.4))
        : Icon(widget.icon ?? Icons.check_rounded);
    if (widget.outlined) {
      return OutlinedButton.icon(
        style: widget.danger ? OutlinedButton.styleFrom(foregroundColor: c.dText) : null,
        onPressed: onPressed,
        icon: icon,
        label: Text(widget.label),
      );
    }
    return FilledButton.icon(
      style: widget.danger ? FilledButton.styleFrom(backgroundColor: c.danger, foregroundColor: Colors.white) : null,
      onPressed: onPressed,
      icon: icon,
      label: Text(widget.label),
    );
  }
}

class SectionLabel extends StatelessWidget {
  const SectionLabel(this.text, {super.key, this.trailing});

  final String text;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(top: 18, bottom: 8),
    child: trailing == null
        ? Text(text, style: context.text.labelMedium)
        : Row(children: [Expanded(child: Text(text, style: context.text.labelMedium)), trailing!]),
  );
}

Widget vehicleStatusBadge(VehicleStatus status) => switch (status) {
  VehicleStatus.available => const StatusBadge('Available', tone: Tone.ok, icon: Icons.check_circle_rounded),
  VehicleStatus.active => const StatusBadge('On the road', tone: Tone.live, icon: Icons.radio_button_checked_rounded),
  VehicleStatus.inactive => const StatusBadge('Inactive', tone: Tone.neutral, icon: Icons.block_rounded),
  VehicleStatus.underMaintenance => const StatusBadge('Maintenance', tone: Tone.warn, icon: Icons.build_rounded),
};

Widget tripStatusBadge(TripStatus status, {bool overdue = false}) => switch (status) {
  TripStatus.active when overdue => const StatusBadge('Overdue', tone: Tone.bad, icon: Icons.warning_rounded),
  TripStatus.active => const StatusBadge('Live', tone: Tone.live, icon: Icons.radio_button_checked_rounded),
  TripStatus.ended => const StatusBadge('Completed', tone: Tone.ok, icon: Icons.task_alt_rounded),
  TripStatus.cancelled => const StatusBadge('Cancelled', tone: Tone.bad, icon: Icons.cancel_rounded),
};

Widget transferStatusBadge(TransferStatus status, {bool overdue = false}) => switch (status) {
  TransferStatus.pendingDeparture => const StatusBadge('Ready to depart', tone: Tone.info, icon: Icons.schedule_rounded),
  TransferStatus.inTransit when overdue => const StatusBadge('Overdue', tone: Tone.bad, icon: Icons.warning_rounded),
  TransferStatus.inTransit => const StatusBadge('On the way', tone: Tone.live, icon: Icons.local_shipping_rounded),
  TransferStatus.completed => const StatusBadge('Arrived', tone: Tone.ok, icon: Icons.task_alt_rounded),
  TransferStatus.cancelled => const StatusBadge('Cancelled', tone: Tone.bad, icon: Icons.cancel_rounded),
};

/// Asks before a consequential action; returns true when confirmed.
Future<bool> confirmAction(
  BuildContext context, {
  required String title,
  required String message,
  required String confirmLabel,
  String cancelLabel = 'Cancel',
  bool danger = false,
}) async {
  final c = context.dm;
  final result = await showDialog<bool>(
    context: context,
    builder: (context) => AlertDialog(
      title: Text(title),
      content: Text(message),
      actions: [
        TextButton(onPressed: () => Navigator.of(context).pop(false), child: Text(cancelLabel)),
        TextButton(
          style: danger ? TextButton.styleFrom(foregroundColor: c.dText) : null,
          onPressed: () => Navigator.of(context).pop(true),
          child: Text(confirmLabel),
        ),
      ],
    ),
  );
  return result ?? false;
}
