namespace VehicleManagementDM.Api.Common.Time;

/// <summary>Time arithmetic for hours reports (BR-06, RPT-04).</summary>
public static class Hours
{
    /// <summary>UTC instant when a GST calendar day starts (00:00 GST = 20:00 UTC the day before).</summary>
    public static DateTime GstDayStartUtc(DateOnly day) => day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) - Gst.Offset;

    /// <summary>Seconds of [start, end) that fall inside [rangeStart, rangeEnd).</summary>
    public static long Overlap(DateTime start, DateTime end, DateTime rangeStart, DateTime rangeEnd)
    {
        var from = start > rangeStart ? start : rangeStart;
        var to = end < rangeEnd ? end : rangeEnd;
        return to > from ? (long)(to - from).TotalSeconds : 0;
    }

    /// <summary>Splits [start, end) into GST calendar days inside the range: hours crossing midnight count on each day (RPT-04).</summary>
    public static IEnumerable<(DateOnly Day, long Seconds)> SplitByGstDay(DateTime start, DateTime end, DateTime rangeStart, DateTime rangeEnd)
    {
        var from = start > rangeStart ? start : rangeStart;
        var to = end < rangeEnd ? end : rangeEnd;
        if (to <= from)
        {
            yield break;
        }

        var day = DateOnly.FromDateTime(Gst.FromUtc(from).DateTime);
        while (true)
        {
            var dayStart = GstDayStartUtc(day);
            var dayEnd = GstDayStartUtc(day.AddDays(1));
            var seconds = Overlap(from, to, dayStart, dayEnd);
            if (seconds > 0)
            {
                yield return (day, seconds);
            }

            if (dayEnd >= to)
            {
                yield break;
            }

            day = day.AddDays(1);
        }
    }
}
