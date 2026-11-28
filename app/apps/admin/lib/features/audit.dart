import 'dart:convert';

import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../widgets/admin_widgets.dart';

/// Audited record types (AuditInterceptor) with readable names.
const _entityTypes = <String?, String>{
  null: 'Everything',
  'Vehicle': 'Vehicles',
  'VehicleCategory': 'Categories',
  'VehicleType': 'Types',
  'ParkingLot': 'Lots',
  'LotTypeCapacity': 'Lot capacities',
  'Area': 'Areas',
  'Location': 'Locations',
  'Transfer': 'Transfers',
  'User': 'People',
};

/// One record of each type, for list rows ("Vehicle #6").
const _entityNames = <String, String>{
  'Vehicle': 'Vehicle',
  'VehicleCategory': 'Category',
  'VehicleType': 'Type',
  'ParkingLot': 'Lot',
  'LotTypeCapacity': 'Lot capacity',
  'Area': 'Area',
  'Location': 'Location',
  'Transfer': 'Transfer',
  'User': 'Person',
};

/// "currentLotId" → "Current lot id", "PasswordReset" → "Password reset".
String _humanize(String name) {
  final spaced = name.replaceAllMapped(RegExp('(?<=[a-z])(?=[A-Z])'), (_) => ' ');
  return spaced.isEmpty ? spaced : spaced[0].toUpperCase() + spaced.substring(1).toLowerCase();
}

/// A-23: who changed what and when (AUD-01, AUD-02); filter by record type and dates; tap for before/after.
class AuditLogScreen extends ConsumerStatefulWidget {
  const AuditLogScreen({super.key});

  @override
  ConsumerState<AuditLogScreen> createState() => _AuditLogScreenState();
}

class _AuditLogScreenState extends ConsumerState<AuditLogScreen> {
  String? _entityType;
  DateRange _range = DateRange.lastDays(7);

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Audit log')),
    body: Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(DmSize.gutter, 4, DmSize.gutter, 8),
          child: Row(
            children: [
              Expanded(
                child: DropdownButtonFormField<String?>(
                  initialValue: _entityType,
                  isExpanded: true,
                  decoration: const InputDecoration(labelText: 'Records'),
                  items: [for (final e in _entityTypes.entries) DropdownMenuItem(value: e.key, child: Text(e.value))],
                  onChanged: (v) => setState(() => _entityType = v),
                ),
              ),
              const SizedBox(width: 10),
              DateRangeButton(range: _range, onChanged: (r) => setState(() => _range = r)),
            ],
          ),
        ),
        Expanded(
          child: PagedListView<AuditEntry>(
            key: ValueKey('$_entityType|${_range.from}|${_range.to}'),
            fetch: (page) => ref
                .read(adminReposProvider)
                .auditLog(entityType: _entityType, fromGst: _range.from, toGst: _range.to, page: page),
            empty: const EmptyView(icon: Icons.history_rounded, title: 'No changes', message: 'Nothing was changed in these dates.'),
            itemBuilder: (context, entry) => ListRow(
              leading: IconTile(_icon(entry.action), tone: Tone.neutral, size: 38),
              title: '${_humanize(entry.action)} · ${_entityNames[entry.entityType] ?? entry.entityType} #${entry.entityId}',
              subtitle: '${Gst.dateTime(entry.atUtc)} · ${entry.user.fullName} (${entry.user.loginId})',
              onTap: () => _showEntry(context, entry),
            ),
          ),
        ),
      ],
    ),
  );

  static IconData _icon(String action) => switch (action) {
    'Create' => Icons.add_circle_rounded,
    'Delete' => Icons.delete_rounded,
    'Deactivate' => Icons.block_rounded,
    'Reactivate' => Icons.check_circle_rounded,
    'PasswordReset' || 'PasswordChange' => Icons.key_rounded,
    _ => Icons.edit_rounded,
  };

  void _showEntry(BuildContext context, AuditEntry entry) {
    final keys = {...?entry.before?.keys, ...?entry.after?.keys}.toList()..sort();
    String show(Object? value) => value == null ? '—' : (value is String ? value : jsonEncode(value));
    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (context) => DraggableScrollableSheet(
        expand: false,
        initialChildSize: .6,
        builder: (context, controller) => ListView(
          controller: controller,
          padding: const EdgeInsets.fromLTRB(DmSize.gutter, 0, DmSize.gutter, 24),
          children: [
            Text('${_humanize(entry.action)} · ${_entityNames[entry.entityType] ?? entry.entityType} #${entry.entityId}', style: context.text.titleLarge),
            Text('${Gst.dateTime(entry.atUtc)} · ${entry.user.fullName}', style: context.text.bodyMedium),
            const SizedBox(height: 12),
            if (keys.isEmpty) Text('No field details.', style: context.text.bodyMedium),
            for (final key in keys)
              Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: AppCard(
                  padding: const EdgeInsets.all(12),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(_humanize(key), style: context.text.labelMedium),
                      const SizedBox(height: 4),
                      if (entry.before != null) Text('Before: ${show(entry.before![key])}', style: context.text.bodySmall),
                      if (entry.after != null) Text('After: ${show(entry.after![key])}', style: context.text.bodyMedium),
                    ],
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}
