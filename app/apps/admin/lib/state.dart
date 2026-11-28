import 'package:dm_core/dm_core.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

// Data loads when a screen opens and on pull to refresh; nothing polls (D-035). After an edit, the screen
// invalidates the providers whose data changed.

final dashboardProvider = FutureProvider.autoDispose<Dashboard>((ref) => ref.watch(adminReposProvider).dashboard());

/// Live trips filter (A-02): origin lot and vehicle type.
class LiveFilter {
  const LiveFilter({this.lotId, this.typeId});

  final int? lotId;
  final int? typeId;

  @override
  bool operator ==(Object other) => other is LiveFilter && other.lotId == lotId && other.typeId == typeId;

  @override
  int get hashCode => Object.hash(lotId, typeId);
}

final liveFilterProvider = StateProvider<LiveFilter>((ref) => const LiveFilter());

final liveTripsProvider = FutureProvider.autoDispose<List<TripSummary>>((ref) {
  final filter = ref.watch(liveFilterProvider);
  return ref.watch(adminReposProvider).liveTrips(lotId: filter.lotId, typeId: filter.typeId);
});

/// Open transfers for Live (A-03): waiting to depart and on the way.
final openTransfersProvider = FutureProvider.autoDispose<(List<Transfer>, List<Transfer>)>((ref) async {
  final repos = ref.watch(adminReposProvider);
  final results = await Future.wait([
    repos.transfers(status: TransferStatus.pendingDeparture),
    repos.transfers(status: TransferStatus.inTransit),
  ]);
  return (results[0].items, results[1].items);
});

final tripProvider = FutureProvider.autoDispose.family<TripDetail, int>((ref, id) => ref.watch(reposProvider).trip(id));

final transferProvider = FutureProvider.autoDispose.family<Transfer, int>(
  (ref, id) => ref.watch(reposProvider).transfer(id),
);

/// Every lot, inactive ones included (Manage, filters).
final allLotsProvider = FutureProvider.autoDispose<List<Lot>>((ref) => ref.watch(adminReposProvider).lots());

/// Active lots only (pickers: new vehicle, transfer, place a vehicle).
final activeLotsProvider = FutureProvider.autoDispose<List<Lot>>(
  (ref) async => (await ref.watch(allLotsProvider.future)).where((l) => l.isActive).toList(),
);

final lotProvider = FutureProvider.autoDispose.family<Lot, int>((ref, id) => ref.watch(adminReposProvider).lot(id));

/// Categories with their types, inactive ones included.
final categoriesProvider = FutureProvider.autoDispose<List<VehicleCategory>>(
  (ref) => ref.watch(adminReposProvider).categories(),
);

final areasProvider = FutureProvider.autoDispose<List<Area>>((ref) => ref.watch(adminReposProvider).areas());

final vehicleProvider = FutureProvider.autoDispose.family<VehicleDetail, int>(
  (ref, id) => ref.watch(adminReposProvider).vehicle(id),
);

final staffMemberProvider = FutureProvider.autoDispose.family<StaffMember, int>(
  (ref, id) => ref.watch(adminReposProvider).staffMember(id),
);

final adminsProvider = FutureProvider.autoDispose<List<AdminAccount>>((ref) => ref.watch(adminReposProvider).admins());

final adminAccountProvider = FutureProvider.autoDispose.family<AdminAccount, int>(
  (ref, id) => ref.watch(adminReposProvider).admin(id),
);

final settingsProvider = FutureProvider.autoDispose<AppSettings>((ref) => ref.watch(adminReposProvider).settings());

/// Every vehicle type as "Category · Type", active categories and types only (pickers).
final activeTypeOptionsProvider = FutureProvider.autoDispose<List<TypeOption>>((ref) async {
  final categories = await ref.watch(categoriesProvider.future);
  return [
    for (final category in categories.where((c) => c.isActive))
      for (final type in category.types.where((t) => t.isActive))
        TypeOption(id: type.id, categoryId: category.id, label: '${category.name} · ${type.name}'),
  ];
});

class TypeOption {
  const TypeOption({required this.id, required this.categoryId, required this.label});

  final int id;
  final int categoryId;
  final String label;
}
