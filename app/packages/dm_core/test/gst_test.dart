import 'package:dm_core/dm_core.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('UTC times show in GST (UTC+4)', () {
    final utc = DateTime.utc(2026, 11, 27, 21, 30);

    expect(Gst.time(utc), '01:30');
    expect(Gst.fromUtc(utc).day, 28);
  });

  test('date filters are GST dates', () {
    expect(Gst.dateParam(DateTime(2026, 11, 5)), '2026-11-05');
  });

  test('durations format for totals and running clocks', () {
    expect(formatDuration(const Duration(hours: 2, minutes: 5, seconds: 40)), '2h 05m');
    expect(formatDuration(const Duration(minutes: 12)), '12m');
    expect(formatDuration(const Duration(seconds: 45)), '45s');
    expect(formatDuration(const Duration(seconds: -5)), '0s');
    expect(formatClock(const Duration(hours: 1, minutes: 2, seconds: 3)), '01:02:03');
  });
}
