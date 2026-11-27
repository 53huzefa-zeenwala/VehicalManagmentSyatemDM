import 'json.dart';

// Shapes follow API_DOCUMENTATION.md v1.0 (field names are the API's camelCase names).

enum UserRole { admin, staff }

enum VehicleStatus { available, active, inactive, underMaintenance }

enum TripStatus { active, ended, cancelled }

enum SegmentKind { start, vehicleSwap, handOver }

enum CancelCategory { driver, vehicle, technical, other }

enum TransferStatus { pendingDeparture, inTransit, completed, cancelled }

enum LotType { open, underground, other }

enum NotificationType {
  tripReminder,
  tripOverdue,
  transferReminder,
  transferOverdue,
  transferAwaitingDeparture,
  transferAwaitingArrival,
  tripCancelled,
  vehicleReleased,
  unknown,
}

/// The API's enum names are PascalCase; Dart's are camelCase.
String apiName(Enum value) => value.name[0].toUpperCase() + value.name.substring(1);

/// Non-blocking warning or block reason: `{ code, message }`.
class Notice {
  const Notice({required this.code, required this.message});

  factory Notice.fromJson(Json json) => Notice(code: str(json, 'code'), message: str(json, 'message'));

  final String code;
  final String message;
}

class MonthStats {
  const MonthStats({required this.trips, required this.totalSeconds});

  factory MonthStats.fromJson(Json json) =>
      MonthStats(trips: integer(json, 'trips'), totalSeconds: integer(json, 'totalSeconds'));

  final int trips;
  final int totalSeconds;
}

/// `GET auth/me` and the sign-in response.
class Me {
  const Me({
    required this.id,
    required this.loginId,
    required this.role,
    required this.fullName,
    required this.mobile,
    this.email,
    this.licenceNumber,
    this.photoUrl,
    this.hasActiveTrip = false,
    this.thisMonth,
  });

  factory Me.fromJson(Json json) => Me(
    id: integer(json, 'id'),
    loginId: str(json, 'loginId'),
    role: enumOf(UserRole.values, json['role'], UserRole.staff),
    fullName: str(json, 'fullName'),
    mobile: str(json, 'mobile'),
    email: strOrNull(json, 'email'),
    licenceNumber: strOrNull(json, 'licenceNumber'),
    photoUrl: strOrNull(json, 'photoUrl'),
    hasActiveTrip: boolean(json, 'hasActiveTrip'),
    thisMonth: json['thisMonth'] == null ? null : MonthStats.fromJson(asJson(json['thisMonth'])),
  );

  final int id;
  final String loginId;
  final UserRole role;
  final String fullName;
  final String mobile;
  final String? email;
  final String? licenceNumber;
  final String? photoUrl;
  final bool hasActiveTrip;
  final MonthStats? thisMonth;

  String get firstName => fullName.split(' ').first;
}

class LotRef {
  const LotRef({required this.id, required this.code, required this.name});

  factory LotRef.fromJson(Json json) => LotRef(id: integer(json, 'id'), code: str(json, 'code'), name: str(json, 'name'));

  static LotRef? maybe(Object? json) => json == null ? null : LotRef.fromJson(asJson(json));

  final int id;
  final String code;
  final String name;
}

class StaffRef {
  const StaffRef({required this.id, required this.loginId, required this.fullName, this.photoUrl});

  factory StaffRef.fromJson(Json json) => StaffRef(
    id: integer(json, 'id'),
    loginId: str(json, 'loginId'),
    fullName: str(json, 'fullName'),
    photoUrl: strOrNull(json, 'photoUrl'),
  );

  static StaffRef? maybe(Object? json) => json == null ? null : StaffRef.fromJson(asJson(json));

  final int id;
  final String loginId;
  final String fullName;
  final String? photoUrl;
}

class VehicleRef {
  const VehicleRef({required this.id, required this.registrationNumber, required this.category, required this.type});

  factory VehicleRef.fromJson(Json json) => VehicleRef(
    id: integer(json, 'id'),
    registrationNumber: str(json, 'registrationNumber'),
    category: str(json, 'category'),
    type: str(json, 'type'),
  );

  final int id;
  final String registrationNumber;
  final String category;
  final String type;
}

class VehicleCard extends VehicleRef {
  const VehicleCard({
    required super.id,
    required super.registrationNumber,
    required super.category,
    required super.type,
    required this.brand,
    required this.model,
    required this.colour,
    required this.status,
    this.photoUrl,
    this.currentLot,
  });

  factory VehicleCard.fromJson(Json json) => VehicleCard(
    id: integer(json, 'id'),
    registrationNumber: str(json, 'registrationNumber'),
    category: str(json, 'category'),
    type: str(json, 'type'),
    brand: str(json, 'brand'),
    model: str(json, 'model'),
    colour: str(json, 'colour'),
    photoUrl: strOrNull(json, 'photoUrl'),
    status: enumOf(VehicleStatus.values, json['status'], VehicleStatus.inactive),
    currentLot: LotRef.maybe(json['currentLot']),
  );

  final String brand;
  final String model;
  final String colour;
  final String? photoUrl;
  final VehicleStatus status;

  /// Null when the vehicle is not at a lot or is out on a trip.
  final LotRef? currentLot;
}

/// `GET vehicles/lookup`: the vehicle plus whether this staff member can start a trip with it now.
class VehicleLookup {
  const VehicleLookup({required this.vehicle, required this.canStart, this.blockReason});

  factory VehicleLookup.fromJson(Json json) => VehicleLookup(
    vehicle: VehicleCard.fromJson(asJson(json['vehicle'])),
    canStart: boolean(json, 'canStart'),
    blockReason: json['blockReason'] == null ? null : Notice.fromJson(asJson(json['blockReason'])),
  );

  final VehicleCard vehicle;
  final bool canStart;
  final Notice? blockReason;
}

class DestinationRef {
  const DestinationRef({required this.locationId, required this.area, required this.location});

  factory DestinationRef.fromJson(Json json) =>
      DestinationRef(locationId: integer(json, 'locationId'), area: str(json, 'area'), location: str(json, 'location'));

  final int locationId;
  final String area;
  final String location;

  String get label => '$location · $area';
}

class TripSegment {
  const TripSegment({
    required this.seq,
    required this.kind,
    required this.staff,
    required this.vehicle,
    required this.startedAtUtc,
    this.endedAtUtc,
    this.reason,
    this.outgoingVehicleLot,
    this.outgoingVehicleNotAtLot = false,
  });

  factory TripSegment.fromJson(Json json) => TripSegment(
    seq: integer(json, 'seq'),
    kind: enumOf(SegmentKind.values, json['kind'], SegmentKind.start),
    staff: StaffRef.fromJson(asJson(json['staff'])),
    vehicle: VehicleRef.fromJson(asJson(json['vehicle'])),
    startedAtUtc: utc(json, 'startedAtUtc'),
    endedAtUtc: utcOrNull(json, 'endedAtUtc'),
    reason: strOrNull(json, 'reason'),
    outgoingVehicleLot: LotRef.maybe(json['outgoingVehicleLot']),
    outgoingVehicleNotAtLot: boolean(json, 'outgoingVehicleNotAtLot'),
  );

  final int seq;
  final SegmentKind kind;
  final StaffRef staff;
  final VehicleRef vehicle;
  final DateTime startedAtUtc;
  final DateTime? endedAtUtc;
  final String? reason;
  final LotRef? outgoingVehicleLot;
  final bool outgoingVehicleNotAtLot;
}

class TripCancel {
  const TripCancel({required this.category, required this.note});

  factory TripCancel.fromJson(Json json) =>
      TripCancel(category: enumOf(CancelCategory.values, json['category'], CancelCategory.other), note: str(json, 'note'));

  final CancelCategory category;
  final String note;
}

class TripDetail {
  const TripDetail({
    required this.id,
    required this.status,
    required this.startedAtUtc,
    required this.originLot,
    required this.destination,
    required this.currentStaff,
    required this.currentVehicle,
    required this.segments,
    this.endedAtUtc,
    this.totalSeconds,
    this.isOverdue = false,
    this.nextReminderAtUtc,
    this.signatureUrl,
    this.returnLot,
    this.cancel,
    this.warnings = const [],
  });

  factory TripDetail.fromJson(Json json) => TripDetail(
    id: integer(json, 'id'),
    status: enumOf(TripStatus.values, json['status'], TripStatus.ended),
    startedAtUtc: utc(json, 'startedAtUtc'),
    endedAtUtc: utcOrNull(json, 'endedAtUtc'),
    totalSeconds: intOrNull(json, 'totalSeconds'),
    isOverdue: boolean(json, 'isOverdue'),
    nextReminderAtUtc: utcOrNull(json, 'nextReminderAtUtc'),
    originLot: LotRef.fromJson(asJson(json['originLot'])),
    destination: DestinationRef.fromJson(asJson(json['destination'])),
    currentStaff: StaffRef.fromJson(asJson(json['currentStaff'])),
    currentVehicle: VehicleCard.fromJson(asJson(json['currentVehicle'])),
    signatureUrl: strOrNull(json, 'signatureUrl'),
    segments: asList(json['segments'], TripSegment.fromJson),
    returnLot: LotRef.maybe(json['returnLot']),
    cancel: json['cancel'] == null ? null : TripCancel.fromJson(asJson(json['cancel'])),
    warnings: asList(json['warnings'], Notice.fromJson),
  );

  final int id;
  final TripStatus status;
  final DateTime startedAtUtc;
  final DateTime? endedAtUtc;
  final int? totalSeconds;
  final bool isOverdue;
  final DateTime? nextReminderAtUtc;
  final LotRef originLot;
  final DestinationRef destination;
  final StaffRef currentStaff;
  final VehicleCard currentVehicle;
  final String? signatureUrl;
  final List<TripSegment> segments;
  final LotRef? returnLot;
  final TripCancel? cancel;
  final List<Notice> warnings;

  bool get isActive => status == TripStatus.active;
}

class TripSummary {
  const TripSummary({
    required this.id,
    required this.status,
    required this.startedAtUtc,
    required this.originLot,
    required this.destination,
    required this.currentStaff,
    required this.currentVehicle,
    required this.segmentCount,
    this.endedAtUtc,
    this.totalSeconds,
    this.isOverdue = false,
    this.returnLot,
  });

  factory TripSummary.fromJson(Json json) => TripSummary(
    id: integer(json, 'id'),
    status: enumOf(TripStatus.values, json['status'], TripStatus.ended),
    startedAtUtc: utc(json, 'startedAtUtc'),
    endedAtUtc: utcOrNull(json, 'endedAtUtc'),
    totalSeconds: intOrNull(json, 'totalSeconds'),
    isOverdue: boolean(json, 'isOverdue'),
    originLot: LotRef.fromJson(asJson(json['originLot'])),
    returnLot: LotRef.maybe(json['returnLot']),
    destination: DestinationRef.fromJson(asJson(json['destination'])),
    currentStaff: StaffRef.fromJson(asJson(json['currentStaff'])),
    currentVehicle: VehicleRef.fromJson(asJson(json['currentVehicle'])),
    segmentCount: integer(json, 'segmentCount'),
  );

  final int id;
  final TripStatus status;
  final DateTime startedAtUtc;
  final DateTime? endedAtUtc;
  final int? totalSeconds;
  final bool isOverdue;
  final LotRef originLot;
  final LotRef? returnLot;
  final DestinationRef destination;
  final StaffRef currentStaff;
  final VehicleRef currentVehicle;
  final int segmentCount;
}

class PagedResult<T> {
  const PagedResult({required this.items, required this.page, required this.pageSize, required this.totalCount});

  factory PagedResult.fromJson(Json json, T Function(Json json) parse) => PagedResult(
    items: asList(json['items'], parse),
    page: integer(json, 'page'),
    pageSize: integer(json, 'pageSize'),
    totalCount: integer(json, 'totalCount'),
  );

  final List<T> items;
  final int page;
  final int pageSize;
  final int totalCount;

  bool get hasMore => page * pageSize < totalCount;
}

class LotTotals {
  const LotTotals({
    required this.capacity,
    required this.occupied,
    required this.available,
    required this.isFull,
    required this.isOverCapacity,
  });

  factory LotTotals.fromJson(Json json) => LotTotals(
    capacity: integer(json, 'capacity'),
    occupied: integer(json, 'occupied'),
    available: integer(json, 'available'),
    isFull: boolean(json, 'isFull'),
    isOverCapacity: boolean(json, 'isOverCapacity'),
  );

  final int capacity;
  final int occupied;

  /// Can be negative when the lot is over capacity.
  final int available;
  final bool isFull;
  final bool isOverCapacity;

  double get fill => capacity <= 0 ? 0 : (occupied / capacity).clamp(0, 1).toDouble();
}

class LotTypeOccupancy {
  const LotTypeOccupancy({
    required this.vehicleTypeId,
    required this.type,
    required this.category,
    required this.occupied,
    this.capacity,
    this.available,
  });

  factory LotTypeOccupancy.fromJson(Json json) => LotTypeOccupancy(
    vehicleTypeId: integer(json, 'vehicleTypeId'),
    type: str(json, 'type'),
    category: str(json, 'category'),
    capacity: intOrNull(json, 'capacity'),
    occupied: integer(json, 'occupied'),
    available: intOrNull(json, 'available'),
  );

  final int vehicleTypeId;
  final String type;
  final String category;

  /// Null when the lot sets no capacity for this type.
  final int? capacity;
  final int occupied;
  final int? available;
}

class Lot {
  const Lot({
    required this.id,
    required this.code,
    required this.name,
    required this.address,
    required this.latitude,
    required this.longitude,
    required this.contactName,
    required this.contactNumber,
    required this.lotType,
    required this.total,
    required this.byType,
    this.opensAt,
    this.closesAt,
  });

  factory Lot.fromJson(Json json) => Lot(
    id: integer(json, 'id'),
    code: str(json, 'code'),
    name: str(json, 'name'),
    address: str(json, 'address'),
    latitude: dbl(json, 'latitude'),
    longitude: dbl(json, 'longitude'),
    contactName: str(json, 'contactName'),
    contactNumber: str(json, 'contactNumber'),
    opensAt: strOrNull(json, 'opensAt'),
    closesAt: strOrNull(json, 'closesAt'),
    lotType: enumOf(LotType.values, json['lotType'], LotType.other),
    total: LotTotals.fromJson(asJson(json['total'])),
    byType: asList(json['byType'], LotTypeOccupancy.fromJson),
  );

  final int id;
  final String code;
  final String name;
  final String address;
  final double latitude;
  final double longitude;
  final String contactName;
  final String contactNumber;
  final String? opensAt;
  final String? closesAt;
  final LotType lotType;
  final LotTotals total;
  final List<LotTypeOccupancy> byType;

  String get hours => opensAt == null || closesAt == null ? 'Open 24 hours' : '$opensAt – $closesAt';

  LotRef get ref => LotRef(id: id, code: code, name: name);
}

class Location {
  const Location({required this.id, required this.name});

  factory Location.fromJson(Json json) => Location(id: integer(json, 'id'), name: str(json, 'name'));

  final int id;
  final String name;
}

class Area {
  const Area({required this.id, required this.name, required this.locations});

  factory Area.fromJson(Json json) =>
      Area(id: integer(json, 'id'), name: str(json, 'name'), locations: asList(json['locations'], Location.fromJson));

  final int id;
  final String name;
  final List<Location> locations;
}

class Transfer {
  const Transfer({
    required this.id,
    required this.status,
    required this.vehicle,
    required this.fromLot,
    required this.toLot,
    required this.createdBy,
    required this.createdAtUtc,
    this.note,
    this.driver,
    this.departedAtUtc,
    this.arrivedBy,
    this.arrivedAtUtc,
    this.drivingSeconds,
    this.isOverdue = false,
    this.warnings = const [],
  });

  factory Transfer.fromJson(Json json) => Transfer(
    id: integer(json, 'id'),
    status: enumOf(TransferStatus.values, json['status'], TransferStatus.cancelled),
    vehicle: VehicleRef.fromJson(asJson(json['vehicle'])),
    fromLot: LotRef.fromJson(asJson(json['fromLot'])),
    toLot: LotRef.fromJson(asJson(json['toLot'])),
    note: strOrNull(json, 'note'),
    createdBy: StaffRef.fromJson(asJson(json['createdBy'])),
    createdAtUtc: utc(json, 'createdAtUtc'),
    driver: StaffRef.maybe(json['driver']),
    departedAtUtc: utcOrNull(json, 'departedAtUtc'),
    arrivedBy: StaffRef.maybe(json['arrivedBy']),
    arrivedAtUtc: utcOrNull(json, 'arrivedAtUtc'),
    drivingSeconds: intOrNull(json, 'drivingSeconds'),
    isOverdue: boolean(json, 'isOverdue'),
    warnings: asList(json['warnings'], Notice.fromJson),
  );

  final int id;
  final TransferStatus status;
  final VehicleRef vehicle;
  final LotRef fromLot;
  final LotRef toLot;
  final String? note;
  final StaffRef createdBy;
  final DateTime createdAtUtc;
  final StaffRef? driver;
  final DateTime? departedAtUtc;
  final StaffRef? arrivedBy;
  final DateTime? arrivedAtUtc;
  final int? drivingSeconds;
  final bool isOverdue;
  final List<Notice> warnings;
}

class TransferTasks {
  const TransferTasks({required this.awaitingDeparture, required this.inTransit});

  factory TransferTasks.fromJson(Json json) => TransferTasks(
    awaitingDeparture: asList(json['awaitingDeparture'], Transfer.fromJson),
    inTransit: asList(json['inTransit'], Transfer.fromJson),
  );

  final List<Transfer> awaitingDeparture;
  final List<Transfer> inTransit;

  bool get isEmpty => awaitingDeparture.isEmpty && inTransit.isEmpty;
}

class AppNotification {
  const AppNotification({
    required this.id,
    required this.type,
    required this.title,
    required this.body,
    required this.createdAtUtc,
    this.targetType,
    this.targetId,
    this.readAtUtc,
  });

  factory AppNotification.fromJson(Json json) => AppNotification(
    id: integer(json, 'id'),
    type: enumOf(NotificationType.values, json['type'], NotificationType.unknown),
    title: str(json, 'title'),
    body: str(json, 'body'),
    targetType: strOrNull(json, 'targetType'),
    targetId: intOrNull(json, 'targetId'),
    createdAtUtc: utc(json, 'createdAtUtc'),
    readAtUtc: utcOrNull(json, 'readAtUtc'),
  );

  final int id;
  final NotificationType type;
  final String title;
  final String body;

  /// "Trip" or "Transfer".
  final String? targetType;
  final int? targetId;
  final DateTime createdAtUtc;
  final DateTime? readAtUtc;

  bool get isRead => readAtUtc != null;
}

class NotificationPage extends PagedResult<AppNotification> {
  const NotificationPage({
    required super.items,
    required super.page,
    required super.pageSize,
    required super.totalCount,
    required this.unreadCount,
  });

  factory NotificationPage.fromJson(Json json) => NotificationPage(
    items: asList(json['items'], AppNotification.fromJson),
    page: integer(json, 'page'),
    pageSize: integer(json, 'pageSize'),
    totalCount: integer(json, 'totalCount'),
    unreadCount: integer(json, 'unreadCount'),
  );

  final int unreadCount;
}
