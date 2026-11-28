import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../models/models.dart';
import '../net/api_exception.dart';
import '../repos/repos.dart';
import '../theme/tokens.dart';
import '../time/gst.dart';
import '../widgets/flow.dart';
import '../widgets/widgets.dart';

final notificationsProvider = FutureProvider.autoDispose<NotificationPage>(
  (ref) => ref.watch(reposProvider).notifications(),
);

/// Unread count for the bell; refreshed when a home screen or Notifications reload.
final unreadCountProvider = FutureProvider.autoDispose<int>(
  (ref) async => (await ref.watch(reposProvider).notifications(unreadOnly: true)).unreadCount,
);

/// C-08: in-app notifications (push is off until a Firebase project exists, D-039). Tapping one marks it read and
/// calls [onOpen] with it, so each app opens the trip or transfer its own way.
class NotificationsScreen extends ConsumerWidget {
  const NotificationsScreen({super.key, required this.onOpen});

  final Future<void> Function(BuildContext context, WidgetRef ref, AppNotification item) onOpen;

  void _reload(WidgetRef ref) => ref
    ..invalidate(notificationsProvider)
    ..invalidate(unreadCountProvider);

  Future<void> _open(BuildContext context, WidgetRef ref, AppNotification item) async {
    if (!item.isRead) {
      await ref.read(reposProvider).markNotificationRead(item.id).catchError((Object _) {});
      _reload(ref);
    }
    if (context.mounted && item.targetId != null) {
      await onOpen(context, ref, item);
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

/// Bell with the unread count, for app bars.
class NotificationBell extends ConsumerWidget {
  const NotificationBell({super.key, required this.onPressed});

  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final unread = ref.watch(unreadCountProvider).valueOrNull ?? 0;
    return IconButton(
      tooltip: 'Notifications',
      onPressed: onPressed,
      icon: Badge(
        isLabelVisible: unread > 0,
        label: Text(unread > 99 ? '99+' : '$unread'),
        child: const Icon(Icons.notifications_rounded),
      ),
    );
  }
}

/// Theme choice for the session (System, Light, Dark).
final themeModeProvider = StateProvider<ThemeMode>((ref) => ThemeMode.system);
