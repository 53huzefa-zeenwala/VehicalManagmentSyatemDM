import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../state.dart';
import '../widgets/admin_widgets.dart';

/// A-14: categories with their types; add, rename, activate or deactivate inline (VEH-01). Inactive types can't
/// be picked for new vehicles.
class CategoriesScreen extends ConsumerWidget {
  const CategoriesScreen({super.key});

  Future<void> _run(BuildContext context, WidgetRef ref, Future<void> Function() action, String done) async {
    try {
      await action();
      ref.invalidate(categoriesProvider);
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
    final categories = ref.watch(categoriesProvider);
    final repos = ref.read(adminReposProvider);
    return Scaffold(
      appBar: AppBar(title: const Text('Categories & types')),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () async {
          final name = await askText(context, title: 'New category', label: 'Name (e.g. Truck)', action: 'Add');
          if (name != null && context.mounted) {
            await _run(context, ref, () => repos.createCategory(name), 'Category added.');
          }
        },
        icon: const Icon(Icons.add_rounded),
        label: const Text('Add category'),
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(categoriesProvider);
          await ref.read(categoriesProvider.future);
        },
        child: AsyncBody(
          value: categories,
          onRetry: () => ref.invalidate(categoriesProvider),
          data: (items) => items.isEmpty
              ? ListView(
                  children: const [
                    EmptyView(
                      icon: Icons.category_rounded,
                      title: 'No categories yet',
                      message: 'Start here: add a category (e.g. Truck), then its types. Lots and vehicles use them.',
                    ),
                  ],
                )
              : ListView(
                  padding: const EdgeInsets.fromLTRB(DmSize.gutter, 4, DmSize.gutter, 96),
                  children: [
                    for (final category in items) ...[
                      AppCard(
                        padding: const EdgeInsets.fromLTRB(14, 6, 6, 10),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            Row(
                              children: [
                                Expanded(child: Text(category.name, style: context.text.titleMedium)),
                                activeBadge(category.isActive),
                                _Menu(
                                  isActive: category.isActive,
                                  onRename: () async {
                                    final name = await askText(context, title: 'Rename category', label: 'Name', initial: category.name);
                                    if (name != null && context.mounted) {
                                      await _run(
                                        context,
                                        ref,
                                        () => repos.updateCategory(category.id, name: name, isActive: category.isActive),
                                        'Saved.',
                                      );
                                    }
                                  },
                                  onToggle: () => _run(
                                    context,
                                    ref,
                                    () => repos.updateCategory(category.id, name: category.name, isActive: !category.isActive),
                                    category.isActive ? 'Category deactivated.' : 'Category activated.',
                                  ),
                                ),
                              ],
                            ),
                            for (final type in category.types)
                              Row(
                                children: [
                                  const SizedBox(width: 8),
                                  Icon(Icons.subdirectory_arrow_right_rounded, size: 18, color: context.dm.text3),
                                  const SizedBox(width: 6),
                                  Expanded(
                                    child: Text(
                                      type.name,
                                      style: context.text.bodyMedium?.copyWith(
                                        decoration: type.isActive ? null : TextDecoration.lineThrough,
                                      ),
                                    ),
                                  ),
                                  if (!type.isActive) const StatusBadge('Inactive', tone: Tone.neutral),
                                  _Menu(
                                    isActive: type.isActive,
                                    onRename: () async {
                                      final name = await askText(context, title: 'Rename type', label: 'Name', initial: type.name);
                                      if (name != null && context.mounted) {
                                        await _run(context, ref, () => repos.updateType(type.id, name: name, isActive: type.isActive), 'Saved.');
                                      }
                                    },
                                    onToggle: () => _run(
                                      context,
                                      ref,
                                      () => repos.updateType(type.id, name: type.name, isActive: !type.isActive),
                                      type.isActive ? 'Type deactivated.' : 'Type activated.',
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
                                    title: 'New type in ${category.name}',
                                    label: 'Name (e.g. Garbage truck)',
                                    action: 'Add',
                                  );
                                  if (name != null && context.mounted) {
                                    await _run(context, ref, () => repos.createType(category.id, name), 'Type added.');
                                  }
                                },
                                icon: const Icon(Icons.add_rounded),
                                label: const Text('Add type'),
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

class _Menu extends StatelessWidget {
  const _Menu({required this.isActive, required this.onRename, required this.onToggle});

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
