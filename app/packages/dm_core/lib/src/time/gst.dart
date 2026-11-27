import 'package:intl/intl.dart';

/// All times come from the API in UTC and are shown in Gulf Standard Time (UTC+4, no daylight saving),
/// the same day boundaries the server uses for reports and date filters.
abstract final class Gst {
  static const Duration offset = Duration(hours: 4);

  /// Wall-clock GST for a UTC instant (the result's own time zone flag is meaningless; only display it).
  static DateTime fromUtc(DateTime utc) => utc.toUtc().add(offset);

  static DateTime now() => fromUtc(DateTime.now().toUtc());

  static DateTime today() {
    final now = Gst.now();
    return DateTime(now.year, now.month, now.day);
  }

  /// `yyyy-MM-dd` for the API's `from` / `to` filters (GST dates).
  static String dateParam(DateTime gstDate) => DateFormat('yyyy-MM-dd').format(gstDate);

  static String time(DateTime utc) => DateFormat('HH:mm').format(fromUtc(utc));

  static String date(DateTime utc) => DateFormat('EEE d MMM').format(fromUtc(utc));

  static String dateTime(DateTime utc) => DateFormat('d MMM, HH:mm').format(fromUtc(utc));

  /// "Today", "Yesterday" or "Mon 24 Nov", for grouping lists by GST day.
  static String dayLabel(DateTime utc) {
    final day = fromUtc(utc);
    final date = DateTime(day.year, day.month, day.day);
    final today = Gst.today();
    if (date == today) {
      return 'Today';
    }
    if (date == today.subtract(const Duration(days: 1))) {
      return 'Yesterday';
    }
    return DateFormat('EEE d MMM').format(date);
  }
}

/// "2h 05m", "12m", "45s" — for totals.
String formatDuration(Duration duration) {
  final d = duration.isNegative ? Duration.zero : duration;
  final hours = d.inHours;
  final minutes = d.inMinutes.remainder(60);
  if (hours > 0) {
    return '${hours}h ${minutes.toString().padLeft(2, '0')}m';
  }
  if (d.inMinutes > 0) {
    return '${d.inMinutes}m';
  }
  return '${d.inSeconds}s';
}

/// "01:23:45" — for running timers.
String formatClock(Duration duration) {
  final d = duration.isNegative ? Duration.zero : duration;
  String two(int value) => value.toString().padLeft(2, '0');
  return '${two(d.inHours)}:${two(d.inMinutes.remainder(60))}:${two(d.inSeconds.remainder(60))}';
}
