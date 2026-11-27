import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../state.dart';
import 'scan.dart';

/// S-01 Home: changes with state — no trip (scan hero), trip running (timer hero), overdue (red).
class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  Future<void> _refresh(WidgetRef ref) async {
    ref
      ..invalidate(transferTasksProvider)
      ..invalidate(lotsProvider)
      ..invalidate(unreadCountProvider);
    await Future.wait([
      ref.read(activeTripProvider.notifier).refresh(),
      ref.read(sessionProvider.notifier).refreshMe(),
    ]);
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final me = ref.watch(meProvider);
    final trip = ref.watch(activeTripProvider);
    return RefreshIndicator(
      onRefresh: () => _refresh(ref),
      child: ListView(
        padding: const EdgeInsets.fromLTRB(DmSize.gutter, 8, DmSize.gutter, 24),
        children: [
          _TopBar(me: me),
          const SizedBox(height: 14),
          switch (trip) {
            AsyncData(value: final active?) => _RunningHero(trip: active),
            AsyncData() => _IdleHero(me: me),
            AsyncError(:final error) => AppCard(
              child: ErrorView(
                error: error,
                compact: true,
                onRetry: () => ref.read(activeTripProvider.notifier).refresh(),
              ),
            ),
            _ => const SkeletonList(count: 1, height: 230),
          },
          const _TransferTasksCard(),
          const _LotsNearFullCard(),
        ],
      ),
    );
  }
}

class _TopBar extends ConsumerWidget {
  const _TopBar({required this.me});

  final Me me;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final unread = ref.watch(unreadCountProvider).valueOrNull ?? 0;
    final hour = Gst.now().hour;
    final greeting = hour < 12 ? 'Good morning' : (hour < 17 ? 'Good afternoon' : 'Good evening');
    return Row(
      children: [
        Avatar(name: me.fullName, photoUrl: me.photoUrl),
        const SizedBox(width: 12),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(greeting, style: context.text.bodySmall),
              Text(me.firstName, style: context.text.titleMedium),
            ],
          ),
        ),
        IconButton(
          tooltip: 'Notifications',
          onPressed: () => context.push('/notifications'),
          icon: Badge(
            isLabelVisible: unread > 0,
            label: Text('$unread'),
            child: const Icon(Icons.notifications_rounded),
          ),
        ),
      ],
    );
  }
}

class _Hero extends StatelessWidget {
  const _Hero({required this.children, this.overdue = false});

  final List<Widget> children;
  final bool overdue;

  @override
  Widget build(BuildContext context) {
    final c = context.dm;
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: overdue
            ? LinearGradient(colors: [c.dText, c.danger], begin: Alignment.topLeft, end: Alignment.bottomRight)
            : c.hero,
        borderRadius: BorderRadius.circular(DmSize.rHero),
        boxShadow: c.primaryGlow,
      ),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: children),
    );
  }
}

class _IdleHero extends ConsumerWidget {
  const _IdleHero({required this.me});

  final Me me;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final month = me.thisMonth;
    return _Hero(
      children: [
        Text(DateFormat('EEEE d MMMM').format(Gst.now()), style: dmStyle(12.5, FontWeight.w600, color: Colors.white70)),
        const SizedBox(height: 6),
        Row(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Expanded(
              child: Text('Ready for the next vehicle', style: dmStyle(24, FontWeight.w800, color: Colors.white)),
            ),
            const Illustration('truck', height: 54, color: Colors.white30),
          ],
        ),
        const SizedBox(height: 16),
        FilledButton.icon(
          style: FilledButton.styleFrom(backgroundColor: Colors.white, foregroundColor: const Color(0xFF06503F)),
          onPressed: () => ScanFlow.fromTabBar(context, ref),
          icon: const Icon(Icons.qr_code_scanner_rounded),
          label: const Text('Scan to start trip'),
        ),
        if (month != null) ...[
          const SizedBox(height: 14),
          Row(
            children: [
              _HeroStat(label: 'Trips this month', value: '${month.trips}'),
              const SizedBox(width: 12),
              _HeroStat(label: 'Time out this month', value: formatDuration(Duration(seconds: month.totalSeconds))),
            ],
          ),
        ],
      ],
    );
  }
}

class _HeroStat extends StatelessWidget {
  const _HeroStat({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) => Expanded(
    child: Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(color: Colors.white.withValues(alpha: .14), borderRadius: BorderRadius.circular(16)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(value, style: dmStyle(20, FontWeight.w800, color: Colors.white, tabular: true)),
          Text(label, style: dmStyle(12, FontWeight.w500, color: Colors.white70)),
        ],
      ),
    ),
  );
}

class _RunningHero extends StatelessWidget {
  const _RunningHero({required this.trip});

  final TripDetail trip;

  @override
  Widget build(BuildContext context) {
    final glass = OutlinedButton.styleFrom(
      backgroundColor: Colors.white.withValues(alpha: .16),
      foregroundColor: Colors.white,
      side: BorderSide(color: Colors.white.withValues(alpha: .28)),
    );
    return _Hero(
      overdue: trip.isOverdue,
      children: [
        Row(
          children: [
            Text(
              'Active trip · since ${Gst.time(trip.startedAtUtc)}',
              style: dmStyle(12.5, FontWeight.w600, color: Colors.white70),
            ),
            const Spacer(),
            if (trip.isOverdue)
              const StatusBadge('Overdue', tone: Tone.bad, icon: Icons.warning_rounded)
            else
              const StatusBadge('Live', tone: Tone.live, icon: Icons.radio_button_checked_rounded),
          ],
        ),
        const SizedBox(height: 6),
        Ticker(
          builder: (_, now) => Text(
            formatClock(now.difference(trip.startedAtUtc)),
            style: dmStyle(44, FontWeight.w800, color: Colors.white, tabular: true, spacing: -1.5),
          ),
        ),
        if (trip.nextReminderAtUtc != null)
          Text(
            trip.isOverdue
                ? 'Past the reminder time — end the trip when the vehicle is back'
                : 'Reminder at ${Gst.time(trip.nextReminderAtUtc!)}',
            style: dmStyle(13, FontWeight.w500, color: Colors.white70),
          ),
        const SizedBox(height: 12),
        Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(color: Colors.black.withValues(alpha: .18), borderRadius: BorderRadius.circular(16)),
          child: Row(
            children: [
              PlateBadge(trip.currentVehicle.registrationNumber),
              const SizedBox(width: 10),
              Expanded(
                child: Text(
                  '${trip.currentVehicle.type} → ${trip.destination.location}',
                  overflow: TextOverflow.ellipsis,
                  style: dmStyle(13.5, FontWeight.w600, color: Colors.white),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 14),
        FilledButton.icon(
          style: FilledButton.styleFrom(backgroundColor: Colors.white, foregroundColor: const Color(0xFF06503F)),
          onPressed: () => context.push('/trip/end'),
          icon: const Icon(Icons.flag_rounded),
          label: const Text('End trip'),
        ),
        const SizedBox(height: 10),
        Row(
          children: [
            Expanded(
              child: OutlinedButton.icon(
                style: glass,
                onPressed: () => context.push('/trip/swap'),
                icon: const Icon(Icons.sync_alt_rounded),
                label: const Text('Swap'),
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: OutlinedButton.icon(
                style: glass,
                onPressed: () => context.push('/trip/cancel'),
                icon: const Icon(Icons.close_rounded),
                label: const Text('Cancel'),
              ),
            ),
          ],
        ),
      ],
    );
  }
}

class _TransferTasksCard extends ConsumerWidget {
  const _TransferTasksCard();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tasks = ref.watch(transferTasksProvider).valueOrNull;
    if (tasks == null || tasks.isEmpty) {
      return const SizedBox.shrink();
    }
    final all = [...tasks.awaitingDeparture, ...tasks.inTransit];
    return Padding(
      padding: const EdgeInsets.only(top: DmSize.gap),
      child: AppCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Transfers', style: context.text.titleMedium),
            const SizedBox(height: 4),
            for (final transfer in all.take(4))
              ListTile(
                contentPadding: EdgeInsets.zero,
                leading: IconTile(
                  transfer.status == TransferStatus.inTransit ? Icons.local_shipping_rounded : Icons.swap_horiz_rounded,
                  tone: transfer.status == TransferStatus.inTransit ? Tone.live : Tone.info,
                ),
                title: Text('${transfer.fromLot.name} → ${transfer.toLot.name}', style: context.text.titleSmall),
                subtitle: Text(
                  '${transfer.vehicle.registrationNumber} · '
                  '${transfer.status == TransferStatus.inTransit ? 'On the way' : 'Ready to depart'}',
                  style: context.text.bodySmall,
                ),
                trailing: const Icon(Icons.chevron_right_rounded),
                onTap: () => context.push('/transfers/${transfer.id}'),
              ),
          ],
        ),
      ),
    );
  }
}

class _LotsNearFullCard extends ConsumerWidget {
  const _LotsNearFullCard();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final lots = ref.watch(lotsProvider).valueOrNull;
    final tight = (lots ?? const <Lot>[]).where((l) => l.total.capacity > 0 && l.total.fill >= .85).toList();
    if (tight.isEmpty) {
      return const SizedBox.shrink();
    }
    return Padding(
      padding: const EdgeInsets.only(top: DmSize.gap),
      child: AppCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Lots nearly full', style: context.text.titleMedium),
            const SizedBox(height: 8),
            for (final lot in tight) ...[
              Row(
                children: [
                  Expanded(child: Text(lot.name, style: context.text.titleSmall)),
                  Text('${lot.total.available} free', style: context.text.bodySmall),
                ],
              ),
              const SizedBox(height: 6),
              CapacityBar(fill: lot.total.fill, isFull: lot.total.isFull),
              const SizedBox(height: 10),
            ],
          ],
        ),
      ),
    );
  }
}
