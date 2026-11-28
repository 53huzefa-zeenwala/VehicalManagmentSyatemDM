import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../shell.dart';
import '../state.dart';
import '../widgets/admin_widgets.dart';
import 'live.dart';

/// A-24: the five reports (RPT-02), on screen only (no exports, D-012). Hours that cross midnight are split by
/// GST day; running trips count up to now.
class ReportsHubScreen extends StatelessWidget {
  const ReportsHubScreen({super.key});

  @override
  Widget build(BuildContext context) {
    Widget item(IconData icon, String title, String subtitle, String path) => Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: ListRow(leading: IconTile(icon, size: 40), title: title, subtitle: subtitle, onTap: () => context.push(path)),
    );
    return Scaffold(
      appBar: const TabAppBar(title: 'Reports'),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(DmSize.gutter, 8, DmSize.gutter, 24),
        children: [
          item(Icons.directions_car_rounded, 'Vehicle hours', 'Trip and transfer time per vehicle, per day', '/reports/vehicle-hours'),
          item(Icons.badge_rounded, 'Staff hours', 'Driving time per staff member, per day', '/reports/staff-hours'),
          item(Icons.local_parking_rounded, 'Lot utilisation', 'Occupancy now, trips and transfers in and out', '/reports/lot-utilization'),
          item(Icons.route_rounded, 'Trip log', 'Every trip, including cancelled and handed over', '/reports/trip-log'),
          item(Icons.local_shipping_rounded, 'Transfer log', 'Every transfer between lots', '/reports/transfers'),
          const SizedBox(height: 8),
          Text('Times are UAE time. Hours crossing midnight are split by day.', style: context.text.bodySmall),
        ],
      ),
    );
  }
}

/// Per-day breakdown shown when a row is expanded.
class _Days extends StatelessWidget {
  const _Days({required this.days});

  final List<DaySeconds> days;

  @override
  Widget build(BuildContext context) => Column(
    children: [
      for (final day in days.where((d) => d.seconds > 0)) KeyValueRow(day.date, formatDuration(Duration(seconds: day.seconds))),
    ],
  );
}

class _Totals extends StatelessWidget {
  const _Totals({required this.items});

  final Map<String, String> items;

  @override
  Widget build(BuildContext context) => AppCard(
    child: Row(
      children: [
        for (final e in items.entries)
          Expanded(
            child: Column(
              children: [
                Text(e.value, style: dmStyle(20, FontWeight.w800, color: context.dm.text, tabular: true)),
                Text(e.key, style: context.text.bodySmall, textAlign: TextAlign.center),
              ],
            ),
          ),
      ],
    ),
  );
}

/// R-1: vehicle hours; filters: dates (required, ≤ 366 days), lot, type.
class VehicleHoursScreen extends ConsumerStatefulWidget {
  const VehicleHoursScreen({super.key});

  @override
  ConsumerState<VehicleHoursScreen> createState() => _VehicleHoursScreenState();
}

class _VehicleHoursScreenState extends ConsumerState<VehicleHoursScreen> {
  DateRange _range = DateRange.lastDays(7);
  int? _lotId;
  int? _typeId;

  @override
  Widget build(BuildContext context) {
    final lots = ref.watch(allLotsProvider).valueOrNull ?? const [];
    final types = ref.watch(activeTypeOptionsProvider).valueOrNull ?? const [];
    return Scaffold(
      appBar: AppBar(title: const Text('Vehicle hours')),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(DmSize.gutter, 4, DmSize.gutter, 24),
        children: [
          DateRangeButton(range: _range, onChanged: (r) => setState(() => _range = r)),
          const SizedBox(height: 10),
          Row(
            children: [
              Expanded(
                child: DropdownButtonFormField<int?>(
                  initialValue: _lotId,
                  isExpanded: true,
                  decoration: const InputDecoration(labelText: 'Lot'),
                  items: [
                    const DropdownMenuItem(value: null, child: Text('All lots')),
                    for (final l in lots) DropdownMenuItem(value: l.id, child: Text(l.name)),
                  ],
                  onChanged: (v) => setState(() => _lotId = v),
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: DropdownButtonFormField<int?>(
                  initialValue: _typeId,
                  isExpanded: true,
                  decoration: const InputDecoration(labelText: 'Type'),
                  items: [
                    const DropdownMenuItem(value: null, child: Text('All types')),
                    for (final t in types) DropdownMenuItem(value: t.id, child: Text(t.label)),
                  ],
                  onChanged: (v) => setState(() => _typeId = v),
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          KeyedFutureBuilder<VehicleHoursReport>(
            requestKey: '$_range|$_lotId|$_typeId',
            load: () => ref
                .read(adminReposProvider)
                .vehicleHours(fromGst: _range.from, toGst: _range.to, lotId: _lotId, typeId: _typeId),
            builder: (context, snapshot, retry) {
              if (snapshot.hasError) {
                return ErrorView(error: snapshot.error!, compact: true, onRetry: retry);
              }
              if (!snapshot.hasData) {
                return const SkeletonList(count: 3, height: 64);
              }
              final r = snapshot.data!;
              return Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  _Totals(items: {'Vehicles': '${r.vehicles}', 'Total time': formatDuration(Duration(seconds: r.totalSeconds))}),
                  const SizedBox(height: 10),
                  if (r.rows.isEmpty) const EmptyView(icon: Icons.timer_off_rounded, title: 'No time recorded', message: 'No trips or transfers in these dates.'),
                  for (final row in r.rows) ...[
                    AppCard(
                      padding: EdgeInsets.zero,
                      child: ExpansionTile(
                        shape: const Border(),
                        leading: PlateBadge(row.vehicle.registrationNumber),
                        title: Text(formatDuration(Duration(seconds: row.totalSeconds)), style: context.text.titleSmall),
                        subtitle: Text(
                          'Trips ${formatDuration(Duration(seconds: row.tripSeconds))} · transfers ${formatDuration(Duration(seconds: row.transferSeconds))}',
                          style: context.text.bodySmall,
                        ),
                        childrenPadding: const EdgeInsets.fromLTRB(16, 0, 16, 12),
                        children: [_Days(days: row.days)],
                      ),
                    ),
                    const SizedBox(height: 8),
                  ],
                ],
              );
            },
          ),
        ],
      ),
    );
  }
}

/// R-2: staff (driver) hours; dates required.
class StaffHoursScreen extends ConsumerStatefulWidget {
  const StaffHoursScreen({super.key});

  @override
  ConsumerState<StaffHoursScreen> createState() => _StaffHoursScreenState();
}

class _StaffHoursScreenState extends ConsumerState<StaffHoursScreen> {
  DateRange _range = DateRange.lastDays(7);

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Staff hours')),
    body: ListView(
      padding: const EdgeInsets.fromLTRB(DmSize.gutter, 4, DmSize.gutter, 24),
      children: [
        DateRangeButton(range: _range, onChanged: (r) => setState(() => _range = r)),
        const SizedBox(height: 12),
        KeyedFutureBuilder<StaffHoursReport>(
          requestKey: _range,
          load: () => ref.read(adminReposProvider).staffHours(fromGst: _range.from, toGst: _range.to),
          builder: (context, snapshot, retry) {
            if (snapshot.hasError) {
              return ErrorView(error: snapshot.error!, compact: true, onRetry: retry);
            }
            if (!snapshot.hasData) {
              return const SkeletonList(count: 3, height: 64);
            }
            final r = snapshot.data!;
            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                _Totals(items: {'Staff': '${r.staff}', 'Total time': formatDuration(Duration(seconds: r.totalSeconds))}),
                const SizedBox(height: 10),
                if (r.rows.isEmpty) const EmptyView(icon: Icons.timer_off_rounded, title: 'No time recorded', message: 'No trips or transfers in these dates.'),
                for (final row in r.rows) ...[
                  AppCard(
                    padding: EdgeInsets.zero,
                    child: ExpansionTile(
                      shape: const Border(),
                      leading: Avatar(name: row.staff.fullName, photoUrl: row.staff.photoUrl, size: 38),
                      title: Text('${row.staff.fullName} · ${formatDuration(Duration(seconds: row.totalSeconds))}', style: context.text.titleSmall),
                      subtitle: Text(
                        '${row.trips} trips · ${row.transfers} transfers · ${row.staff.loginId}',
                        style: context.text.bodySmall,
                      ),
                      childrenPadding: const EdgeInsets.fromLTRB(16, 0, 16, 12),
                      children: [
                        KeyValueRow('Trip time', formatDuration(Duration(seconds: row.tripSeconds))),
                        KeyValueRow('Transfer time', formatDuration(Duration(seconds: row.transferSeconds))),
                        const Divider(),
                        _Days(days: row.days),
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
    ),
  );
}

/// R-3: occupancy now per lot and type, plus movements in the period (default today).
class LotUtilizationScreen extends ConsumerStatefulWidget {
  const LotUtilizationScreen({super.key});

  @override
  ConsumerState<LotUtilizationScreen> createState() => _LotUtilizationScreenState();
}

class _LotUtilizationScreenState extends ConsumerState<LotUtilizationScreen> {
  DateRange _range = DateRange.lastDays(1);

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Lot utilisation')),
    body: ListView(
      padding: const EdgeInsets.fromLTRB(DmSize.gutter, 4, DmSize.gutter, 24),
      children: [
        DateRangeButton(range: _range, onChanged: (r) => setState(() => _range = r)),
        const SizedBox(height: 12),
        KeyedFutureBuilder<LotUtilizationReport>(
          requestKey: _range,
          load: () => ref.read(adminReposProvider).lotUtilization(fromGst: _range.from, toGst: _range.to),
          builder: (context, snapshot, retry) {
            if (snapshot.hasError) {
              return ErrorView(error: snapshot.error!, compact: true, onRetry: retry);
            }
            if (!snapshot.hasData) {
              return const SkeletonList(count: 3, height: 110);
            }
            final rows = snapshot.data!.rows;
            if (rows.isEmpty) {
              return const EmptyView(icon: Icons.local_parking_rounded, title: 'No lots', message: 'Add parking lots first.');
            }
            return Column(
              children: [
                for (final row in rows) ...[
                  AppCard(
                    flat: !row.isActive,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Expanded(child: Text(row.lot.name, style: context.text.titleMedium)),
                            Text('${row.total.occupied} / ${row.total.capacity}', style: dmStyle(16, FontWeight.w800, color: context.dm.text, tabular: true)),
                          ],
                        ),
                        const SizedBox(height: 8),
                        CapacityBar(fill: row.total.fill, isFull: row.total.isFull),
                        const SizedBox(height: 8),
                        Wrap(
                          spacing: 6,
                          runSpacing: 6,
                          children: [
                            for (final t in row.byType.where((t) => t.capacity != null))
                              StatusBadge('${t.type} ${t.occupied}/${t.capacity}', tone: (t.available ?? 1) <= 0 ? Tone.warn : Tone.neutral),
                          ],
                        ),
                        const Divider(height: 20),
                        KeyValueRow('Trips started here', '${row.tripsStarted}'),
                        KeyValueRow('Trips returned here', '${row.tripsReturned}'),
                        KeyValueRow('Transfers out / in', '${row.transfersOut} / ${row.transfersIn}'),
                      ],
                    ),
                  ),
                  const SizedBox(height: 10),
                ],
              ],
            );
          },
        ),
        Text('Occupancy is now; trips and transfers are for the dates picked.', style: context.text.bodySmall),
      ],
    ),
  );
}

/// R-4: every trip (completed, cancelled, running, handed over) with filters; tap for the trip.
class TripLogScreen extends ConsumerStatefulWidget {
  const TripLogScreen({super.key});

  @override
  ConsumerState<TripLogScreen> createState() => _TripLogScreenState();
}

class _TripLogScreenState extends ConsumerState<TripLogScreen> {
  DateRange _range = DateRange.lastDays(7);
  TripStatus? _status;
  int? _lotId;

  @override
  Widget build(BuildContext context) {
    final lots = ref.watch(allLotsProvider).valueOrNull ?? const [];
    return Scaffold(
      appBar: AppBar(title: const Text('Trip log')),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(DmSize.gutter, 4, DmSize.gutter, 8),
            child: Column(
              children: [
                Row(
                  children: [
                    DateRangeButton(range: _range, onChanged: (r) => setState(() => _range = r)),
                    const SizedBox(width: 10),
                    Expanded(
                      child: DropdownButtonFormField<int?>(
                        initialValue: _lotId,
                        isExpanded: true,
                        decoration: const InputDecoration(labelText: 'From lot'),
                        items: [
                          const DropdownMenuItem(value: null, child: Text('All lots')),
                          for (final l in lots) DropdownMenuItem(value: l.id, child: Text(l.name)),
                        ],
                        onChanged: (v) => setState(() => _lotId = v),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 8),
                ChoiceChips<TripStatus>(
                  options: const {
                    null: 'All',
                    TripStatus.active: 'Running',
                    TripStatus.ended: 'Completed',
                    TripStatus.cancelled: 'Cancelled',
                  },
                  selected: _status,
                  onSelected: (v) => setState(() => _status = v),
                ),
              ],
            ),
          ),
          Expanded(
            child: PagedListView<TripSummary>(
              key: ValueKey('$_range|$_status|$_lotId'),
              fetch: (page) => ref
                  .read(adminReposProvider)
                  .tripLog(fromGst: _range.from, toGst: _range.to, status: _status, lotId: _lotId, page: page),
              empty: const EmptyView(icon: Icons.route_rounded, title: 'No trips', message: 'No trips match these filters.'),
              itemBuilder: (context, trip) => TripRow(trip: trip),
            ),
          ),
        ],
      ),
    );
  }
}

/// R-5: every transfer with filters; tap for the transfer.
class TransferLogScreen extends ConsumerStatefulWidget {
  const TransferLogScreen({super.key});

  @override
  ConsumerState<TransferLogScreen> createState() => _TransferLogScreenState();
}

class _TransferLogScreenState extends ConsumerState<TransferLogScreen> {
  DateRange _range = DateRange.lastDays(30);
  TransferStatus? _status;

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Transfer log')),
    body: Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(DmSize.gutter, 4, DmSize.gutter, 8),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              DateRangeButton(range: _range, onChanged: (r) => setState(() => _range = r)),
              const SizedBox(height: 8),
              ChoiceChips<TransferStatus>(
                options: const {
                  null: 'All',
                  TransferStatus.pendingDeparture: 'Waiting',
                  TransferStatus.inTransit: 'On the way',
                  TransferStatus.completed: 'Arrived',
                  TransferStatus.cancelled: 'Cancelled',
                },
                selected: _status,
                onSelected: (v) => setState(() => _status = v),
              ),
            ],
          ),
        ),
        Expanded(
          child: PagedListView<Transfer>(
            key: ValueKey('$_range|$_status'),
            fetch: (page) => ref
                .read(adminReposProvider)
                .transferLog(fromGst: _range.from, toGst: _range.to, status: _status, page: page),
            empty: const EmptyView(icon: Icons.local_shipping_rounded, title: 'No transfers', message: 'No transfers match these filters.'),
            itemBuilder: (context, t) => TransferRow(transfer: t),
          ),
        ),
      ],
    ),
  );
}
