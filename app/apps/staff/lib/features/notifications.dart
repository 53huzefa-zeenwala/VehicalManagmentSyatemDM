import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../state.dart';
import 'common.dart';

/// C-08: in-app notifications (push is off until a Firebase project exists, D-039). Tap opens the target.
class NotificationsScreen extends ConsumerWidget {
  const NotificationsScreen({super.key});

  void _reload(WidgetRef ref) => ref
    ..invalidate(notificationsProvider)
    ..invalidate(unreadCountProvider);

  Future<void> _open(BuildContext context, WidgetRef ref, AppNotification item) async {
    if (!item.isRead) {
      await ref.read(reposProvider).markNotificationRead(item.id).catchError((Object _) {});
      _reload(ref);
    }
    if (!context.mounted || item.targetId == null) {
      return;
    }
    switch (item.targetType) {
      case 'Trip':
        final active = ref.read(activeTripProvider).valueOrNull;
        await context.push(active?.id == item.targetId ? '/trip' : '/trips/${item.targetId}');
      case 'Transfer':
        await context.push('/transfers/${item.targetId}');
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final page = ref.watch(notificationsProvider);
    return Scaffold(
      appBar: AppBar(
        title: const Text('Notifications'),
        actions: [
          TextButton(
            onPressed: () async {
              try {
                await ref.read(reposProvider).markAllNotificationsRead();
                _reload(ref);
              } on ApiException catch (error) {
                if (context.mounted) {
                  showError(context, error);
                }
              }
            },
            child: const Text('Read all'),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          _reload(ref);
          await ref.read(notificationsProvider.future);
        },
        child: AsyncBody(
          value: page,
          onRetry: () => _reload(ref),
          data: (data) {
            if (data.items.isEmpty) {
              return ListView(
                children: const [
                  EmptyView(icon: Icons.notifications_rounded, title: 'All quiet', message: 'Reminders and transfer tasks show here.'),
                ],
              );
            }
            final groups = <String, List<AppNotification>>{};
            for (final item in data.items) {
              groups.putIfAbsent(Gst.dayLabel(item.createdAtUtc), () => []).add(item);
            }
            return ListView(
              padding: const EdgeInsets.fromLTRB(DmSize.gutter, 0, DmSize.gutter, 24),
              children: [
                for (final group in groups.entries) ...[
                  SectionLabel(group.key),
                  for (final item in group.value) ...[
                    AppCard(
                      flat: item.isRead,
                      padding: const EdgeInsets.all(12),
                      onTap: () => _open(context, ref, item),
                      child: Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          IconTile(_icon(item.type), tone: _tone(item.type), size: 40),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(item.title, style: context.text.titleSmall),
                                const SizedBox(height: 2),
                                Text(item.body, style: context.text.bodySmall),
                                const SizedBox(height: 4),
                                Text(Gst.time(item.createdAtUtc), style: context.text.labelSmall),
                              ],
                            ),
                          ),
                          if (!item.isRead)
                            Container(
                              width: 9,
                              height: 9,
                              margin: const EdgeInsets.only(top: 4),
                              decoration: BoxDecoration(color: context.dm.danger, shape: BoxShape.circle),
                            ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 8),
                  ],
                ],
              ],
            );
          },
        ),
      ),
    );
  }

  static IconData _icon(NotificationType type) => switch (type) {
    NotificationType.tripReminder || NotificationType.tripOverdue => Icons.schedule_rounded,
    NotificationType.transferAwaitingDeparture ||
    NotificationType.transferAwaitingArrival ||
    NotificationType.transferReminder ||
    NotificationType.transferOverdue => Icons.local_shipping_rounded,
    _ => Icons.info_rounded,
  };

  static Tone _tone(NotificationType type) => switch (type) {
    NotificationType.tripOverdue || NotificationType.transferOverdue => Tone.bad,
    NotificationType.tripReminder || NotificationType.transferReminder => Tone.live,
    _ => Tone.info,
  };
}
