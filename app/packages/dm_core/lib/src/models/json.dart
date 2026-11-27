/// Small helpers for reading the API's JSON (camelCase, ISO-8601 UTC times, enums as strings).
typedef Json = Map<String, Object?>;

Json asJson(Object? value) => value is Map<String, Object?> ? value : const {};

List<T> asList<T>(Object? value, T Function(Json json) parse) =>
    value is List ? value.whereType<Map<String, Object?>>().map(parse).toList() : const [];

String str(Json json, String key) => json[key] as String? ?? '';

String? strOrNull(Json json, String key) => json[key] as String?;

int integer(Json json, String key) => (json[key] as num?)?.toInt() ?? 0;

int? intOrNull(Json json, String key) => (json[key] as num?)?.toInt();

double dbl(Json json, String key) => (json[key] as num?)?.toDouble() ?? 0;

bool boolean(Json json, String key) => json[key] as bool? ?? false;

DateTime utc(Json json, String key) => DateTime.parse(json[key]! as String).toUtc();

DateTime? utcOrNull(Json json, String key) {
  final value = json[key] as String?;
  return value == null ? null : DateTime.parse(value).toUtc();
}

/// Reads an enum sent as a string; unknown values fall back to [fallback] so a newer server never crashes the app.
T enumOf<T extends Enum>(List<T> values, Object? raw, T fallback) {
  for (final value in values) {
    if (value.name.toLowerCase() == (raw as String?)?.toLowerCase()) {
      return value;
    }
  }
  return fallback;
}
