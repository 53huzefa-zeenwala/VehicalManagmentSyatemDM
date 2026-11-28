import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../state.dart';
import '../widgets/admin_widgets.dart';

/// A-20: destination areas and their locations (DST-01); add, rename, activate or deactivate inline. Staff pick
/// a location when they start a trip.
class DestinationsScreen extends ConsumerWidget {
  const DestinationsScreen({super.key});

  Future<void> _run(BuildContext context, WidgetRef ref, Future<void> Function() action, String done) async {
    try {
      await action();
      ref.invalidate(areasProvider);
      if (context.mounted) {
        showMessage(context, done);
      }
    } on ApiException catch (error) {
      if (context.mounted) {
        showError(context, error);
      }
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final areas = ref.watch(areasProvider);
    final repos = ref.read(adminReposProvider);
    return Scaffold(
      appBar: AppBar(title: const Text('Destinations')),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () async {
          final name = await askText(context, title: 'New area', label: 'Area (e.g. Deira)', action: 'Add');
          if (name != null && context.mounted) {
            await _run(context, ref, () => repos.createArea(name), 'Area added.');
          }
        },
        icon: const Icon(Icons.add_rounded),
        label: const Text('Add area'),
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(areasProvider);
          await ref.read(areasProvider.future);
        },
        child: AsyncBody(
          value: areas,
          onRetry: () => ref.invalidate(areasProvider),
          data: (items) => items.isEmpty
              ? ListView(
                  children: const [
                    EmptyView(
                      icon: Icons.place_rounded,
                      title: 'No destinations yet',
                      message: 'Add areas and their locations. Staff pick one when they start a trip.',
                    ),
                  ],
                )
              : ListView(
                  padding: const EdgeInsets.fromLTRB(DmSize.gutter, 4, DmSize.gutter, 96),
                  children: [
                    for (final area in items) ...[
                      AppCard(
                        padding: const EdgeInsets.fromLTRB(14, 6, 6, 10),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            Row(
                              children: [
                                Expanded(child: Text(area.name, style: context.text.titleMedium)),
                                activeBadge(area.isActive),
                                _RowMenu(
                                  isActive: area.isActive,
                                  onRename: () async {
                                    final name = await askText(context, title: 'Rename area', label: 'Name', initial: area.name);
                                    if (name != null && context.mounted) {
                                      await _run(context, ref, () => repos.updateArea(area.id, name: name, isActive: area.isActive), 'Saved.');
                                    }
                                  },
                                  onToggle: () => _run(
                                    context,
                                    ref,
                                    () => repos.updateArea(area.id, name: area.name, isActive: !area.isActive),
                                    area.isActive ? 'Area deactivated.' : 'Area activated.',
                                  ),
                                ),
                              ],
                            ),
                            for (final location in area.locations)
                              Row(
                                children: [
                                  const SizedBox(width: 8),
                                  Icon(Icons.place_outlined, size: 18, color: context.dm.text3),
                                  const SizedBox(width: 6),
                                  Expanded(
                                    child: Text(
                                      location.name,
                                      style: context.text.bodyMedium?.copyWith(
                                        decoration: location.isActive ? null : TextDecoration.lineThrough,
                                      ),
                                    ),
                                  ),
                                  if (!location.isActive) const StatusBadge('Inactive', tone: Tone.neutral),
                                  _RowMenu(
                                    isActive: location.isActive,
                                    onRename: () async {
                                      final name = await askText(context, title: 'Rename location', label: 'Name', initial: location.name);
                                      if (name != null && context.mounted) {
                                        await _run(
                                          context,
                                          ref,
                                          () => repos.updateLocation(location.id, name: name, isActive: location.isActive),
                                          'Saved.',
                                        );
                                      }
                                    },
                                    onToggle: () => _run(
                                      context,
                                      ref,
                                      () => repos.updateLocation(location.id, name: location.name, isActive: !location.isActive),
                                      location.isActive ? 'Location deactivated.' : 'Location activated.',
                                    ),
                                  ),
                                ],
                              ),
                            Align(
                              alignment: Alignment.centerLeft,
                              child: TextButton.icon(
                                onPressed: () async {
                                  final name = await askText(
                                    context,
                                    title: 'New location in ${area.name}',
                                    label: 'Location (e.g. Fish Market)',
                                    action: 'Add',
                                  );
                                  if (name != null && context.mounted) {
                                    await _run(context, ref, () => repos.createLocation(area.id, name), 'Location added.');
                                  }
                                },
                                icon: const Icon(Icons.add_rounded),
                                label: const Text('Add location'),
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 10),
                    ],
                  ],
                ),
        ),
      ),
    );
  }
}

class _RowMenu extends StatelessWidget {
  const _RowMenu({required this.isActive, required this.onRename, required this.onToggle});

  final bool isActive;
  final VoidCallback onRename;
  final VoidCallback onToggle;

  @override
  Widget build(BuildContext context) => PopupMenuButton<int>(
    tooltip: 'More',
    onSelected: (value) => value == 0 ? onRename() : onToggle(),
    itemBuilder: (_) => [
      const PopupMenuItem(value: 0, child: Text('Rename')),
      PopupMenuItem(value: 1, child: Text(isActive ? 'Deactivate' : 'Activate')),
    ],
  );
}
