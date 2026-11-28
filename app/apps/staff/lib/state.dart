import 'package:dm_core/dm_core.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// The staff member's running trip (null when none). Shared by the shell's live bar, Home and the trip screens.
/// Loaded on start and refreshed after every trip action or pull-to-refresh (no polling, D-035).
class ActiveTripController extends AsyncNotifier<TripDetail?> {
  @override
  Future<TripDetail?> build() => ref.read(reposProvider).activeTrip();

  Future<void> refresh() async {
    state = await AsyncValue.guard(() => ref.read(reposProvider).activeTrip());
  }

  /// After an action that returns the trip (start, swap): show it without another request.
  void set(TripDetail? trip) => state = AsyncData(trip?.isActive ?? false ? trip : null);
}

final activeTripProvider = AsyncNotifierProvider<ActiveTripController, TripDetail?>(ActiveTripController.new);

final transferTasksProvider = FutureProvider.autoDispose<TransferTasks>(
  (ref) => ref.watch(reposProvider).transferTasks(),
);

final lotsProvider = FutureProvider.autoDispose<List<Lot>>((ref) => ref.watch(reposProvider).lots());

final areasProvider = FutureProvider.autoDispose<List<Area>>((ref) => ref.watch(reposProvider).areas());

final tripDetailProvider = FutureProvider.autoDispose.family<TripDetail, int>(
  (ref, id) => ref.watch(reposProvider).trip(id),
);

final transferProvider = FutureProvider.autoDispose.family<Transfer, int>(
  (ref, id) => ref.watch(reposProvider).transfer(id),
);

enum TripRange { today, week, month }

final tripsProvider = FutureProvider.autoDispose.family<List<TripSummary>, TripRange>((ref, range) async {
  final today = Gst.today();
  final from = switch (range) {
    TripRange.today => today,
    TripRange.week => today.subtract(const Duration(days: 6)),
    TripRange.month => DateTime(today.year, today.month),
  };
  return (await ref.watch(reposProvider).trips(fromGst: from, toGst: today)).items;
});
