namespace VehicleManagementDM.Api.Common.Time;

/// <summary>
/// Gulf Standard Time (UTC+4, no daylight saving), the business time zone. Data is stored in UTC;
/// GST is used only where the business day matters, e.g. the year in IDs and report day-splits (BR-01).
/// </summary>
public static class Gst
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(4);

    public static DateTimeOffset Now(TimeProvider timeProvider) => timeProvider.GetUtcNow().ToOffset(Offset);

    public static DateTimeOffset FromUtc(DateTime utc) =>
        new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToOffset(Offset);
}
