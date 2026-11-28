import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../shell.dart';
import '../state.dart';
import '../widgets/admin_widgets.dart';
import 'live.dart';

/// A-01: today's figures (GST day), trips per hour, then what needs attention. Refreshes on open and on pull
/// (no auto-updates, D-035).
class DashboardScreen extends ConsumerWidget {
  const DashboardScreen({super.key});

  Future<void> _refresh(WidgetRef ref) async {
    ref
      ..invalidate(dashboardProvider)
      ..invalidate(unreadCountProvider);
    await ref.read(dashboardProvider.future);
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final dashboard = ref.watch(dashboardProvider);
    final me = ref.watch(meProvider);
    return Scaffold(
      appBar: const TabAppBar(title: 'Dashboard'),
      body: RefreshIndicator(
        onRefresh: () => _refresh(ref),
        child: AsyncBody(
          value: dashboard,
          onRetry: () => ref.invalidate(dashboardProvider),
          data: (d) => ListView(
            padding: const EdgeInsets.fromLTRB(DmSize.gutter, 8, DmSize.gutter, 24),
            children: [
              Text('Hello, ${me.firstName}', style: context.text.headlineSmall),
              Text('Today · ${Gst.date(DateTime.now().toUtc())}', style: context.text.bodyMedium),
              const SizedBox(height: DmSize.gap),
              _LiveHero(dashboard: d),
              const SizedBox(height: DmSize.gap),
              GridView.count(
                crossAxisCount: 2,
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                mainAxisSpacing: 12,
                crossAxisSpacing: 12,
                childAspectRatio: 1.45,
                children: [
                  KpiTile(label: 'Trips today', value: '${d.tripsToday}', icon: Icons.route_rounded),
                  KpiTile(
                    label: 'Time out today',
                    value: formatDuration(Duration(seconds: d.secondsOutToday)),
                    icon: Icons.timer_rounded,
                    tone: Tone.info,
                  ),
                  KpiTile(
                    label: 'Free spaces',
                    value: '${d.freeSpaces}',
                    icon: Icons.local_parking_rounded,
                    tone: Tone.neutral,
                    onTap: () => context.push('/lots'),
                  ),
                  KpiTile(
                    label: 'Transfers on the way',
                    value: '${d.transfersInTransit}',
                    icon: Icons.local_shipping_rounded,
                    tone: Tone.live,
                    onTap: () => showLiveTab(context, LiveView.transfers),
                  ),
                ],
              ),
              const SectionLabel('Trips started per hour (today)'),
              AppCard(
                child: BarChart(
                  values: [for (final h in d.tripsPerHourToday) h.trips],
                  labels: [for (final h in d.tripsPerHourToday) h.hour.toString().padLeft(2, '0')],
                ),
              ),
              const SectionLabel('Needs attention'),
              _NeedsAttention(attention: d.needsAttention),
            ],
          ),
        ),
      ),
    );
  }
}

class _LiveHero extends StatelessWidget {
  const _LiveHero({required this.dashboard});

  final Dashboard dashboard;

  @override
  Widget build(BuildContext context) => Material(
    color: Colors.transparent,
    child: InkWell(
      borderRadius: BorderRadius.circular(DmSize.rHero),
      onTap: () => showLiveTab(context, LiveView.trips),
      child: Ink(
        padding: const EdgeInsets.all(20),
        decoration: BoxDecoration(gradient: context.dm.hero, borderRadius: BorderRadius.circular(DmSize.rHero)),
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('On a trip now', style: dmStyle(14, FontWeight.w600, color: Colors.white70)),
                  Text(
                    '${dashboard.onTripNow}',
                    style: dmStyle(44, FontWeight.w800, color: Colors.white, tabular: true, spacing: -1),
                  ),
                  Text('Open Live', style: dmStyle(13, FontWeight.w700, color: Colors.white)),
                ],
              ),
            ),
            const Illustration('truck', height: 80, color: Colors.white24),
          ],
        ),
      ),
    ),
  );
}

class _NeedsAttention extends StatelessWidget {
  const _NeedsAttention({required this.attention});

  final NeedsAttention attention;

  @override
  Widget build(BuildContext context) {
    final a = attention;
    if (a.isEmpty) {
      return const AppCard(
        child: EmptyView(icon: Icons.task_alt_rounded, title: 'All clear', message: 'Nothing needs your attention right now.'),
      );
    }
    return Column(
      children: [
        if (a.overdueTrips > 0)
          _AttentionGroup(
            icon: Icons.warning_rounded,
            tone: Tone.bad,
            title: '${a.overdueTrips} overdue ${a.overdueTrips == 1 ? 'trip' : 'trips'}',
            onAll: () => showLiveTab(context, LiveView.trips),
            children: [
              for (final trip in a.overdueTripList)
                ListRow(
                  flat: true,
                  leading: PlateBadge(trip.currentVehicle.registrationNumber),
                  title: trip.currentStaff.fullName,
                  subtitle: 'Since ${Gst.dateTime(trip.startedAtUtc)} · ${trip.destination.label}',
                  onTap: () => context.push('/trips/${trip.id}'),
                ),
            ],
          ),
        if (a.overdueTransfers > 0)
          _AttentionGroup(
            icon: Icons.local_shipping_rounded,
            tone: Tone.bad,
            title: '${a.overdueTransfers} overdue ${a.overdueTransfers == 1 ? 'transfer' : 'transfers'}',
            onAll: () => showLiveTab(context, LiveView.transfers),
            children: const [],
          ),
        if (a.vehiclesNotAtLot > 0)
          _AttentionGroup(
            icon: Icons.wrong_location_rounded,
            tone: Tone.warn,
            title: '${a.vehiclesNotAtLot} ${a.vehiclesNotAtLot == 1 ? 'vehicle' : 'vehicles'} not at a lot',
            onAll: () => context.push('/vehicles?notAtLot=true'),
            children: [
              for (final vehicle in a.vehiclesNotAtLotList)
                ListRow(
                  flat: true,
                  leading: PlateBadge(vehicle.registrationNumber),
                  title: '${vehicle.category} · ${vehicle.type}',
                  subtitle: 'Place it at a lot',
                  onTap: () => context.push('/vehicles/${vehicle.id}'),
                ),
            ],
          ),
        if (a.lotsOverCapacity.isNotEmpty)
          _AttentionGroup(
            icon: Icons.local_parking_rounded,
            tone: Tone.warn,
            title: '${a.lotsOverCapacity.length} ${a.lotsOverCapacity.length == 1 ? 'lot' : 'lots'} over capacity',
            children: [
              for (final lot in a.lotsOverCapacity)
                ListRow(
                  flat: true,
                  title: lot.lot.name,
                  subtitle: '${lot.occupied} parked, room for ${lot.capacity}',
                  onTap: () => context.push('/lots/${lot.lot.id}'),
                ),
            ],
          ),
        if (a.transfersWaiting > 0)
          _AttentionGroup(
            icon: Icons.schedule_rounded,
            tone: Tone.info,
            title: '${a.transfersWaiting} ${a.transfersWaiting == 1 ? 'transfer' : 'transfers'} waiting to depart',
            onAll: () => showLiveTab(context, LiveView.transfers),
            children: [
              for (final t in a.transfersWaitingList)
                ListRow(
                  flat: true,
                  leading: PlateBadge(t.vehicle.registrationNumber),
                  title: '${t.fromLot.name} → ${t.toLot.name}',
                  subtitle: 'Created ${Gst.dateTime(t.createdAtUtc)}',
                  onTap: () => context.push('/transfers/${t.id}'),
                ),
            ],
          ),
      ],
    );
  }
}

class _AttentionGroup extends StatelessWidget {
  const _AttentionGroup({required this.icon, required this.tone, required this.title, required this.children, this.onAll});

  final IconData icon;
  final Tone tone;
  final String title;
  final List<Widget> children;
  final VoidCallback? onAll;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: 12),
    child: AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              IconTile(icon, tone: tone, size: 36),
              const SizedBox(width: 12),
              Expanded(child: Text(title, style: context.text.titleSmall)),
              if (onAll != null) TextButton(onPressed: onAll, child: const Text('See all')),
            ],
          ),
          for (final child in children) ...[const SizedBox(height: 8), child],
        ],
      ),
    ),
  );
}
