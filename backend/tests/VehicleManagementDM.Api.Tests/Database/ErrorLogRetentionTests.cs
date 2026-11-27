using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VehicleManagementDM.Api.BackgroundJobs;
using VehicleManagementDM.Api.Common.Logging;
using VehicleManagementDM.Api.Domain.Entities;
using VehicleManagementDM.Api.Tests.Infrastructure;

namespace VehicleManagementDM.Api.Tests.Database;

/// <summary>Error logs and fallback files older than 30 days are deleted (D-033).</summary>
[Collection(ApiCollection.Name)]
public sealed class ErrorLogRetentionTests(ApiFactory factory)
{
    [Fact]
    public async Task Entries_and_files_older_than_30_days_are_deleted()
    {
        var now = DateTime.UtcNow;
        var oldTrace = "retention-old-" + Guid.NewGuid().ToString("N");
        var recentTrace = "retention-new-" + Guid.NewGuid().ToString("N");
        await factory.WithDbAsync(async db =>
        {
            db.ErrorLogs.AddRange(
                new ErrorLog { AtUtc = now.AddDays(-31), TraceId = oldTrace, Level = "Warning", StatusCode = 400, Code = "TEST", Message = "old" },
                new ErrorLog { AtUtc = now.AddDays(-29), TraceId = recentTrace, Level = "Warning", StatusCode = 400, Code = "TEST", Message = "recent" });
            await db.SaveChangesAsync();
        });

        var fileLog = factory.Services.GetRequiredService<FileErrorLog>();
        Directory.CreateDirectory(fileLog.Directory);
        var oldFile = fileLog.PathFor(now.AddDays(-40));
        var recentFile = fileLog.PathFor(now.AddDays(-2));
        await File.WriteAllTextAsync(oldFile, "{}");
        await File.WriteAllTextAsync(recentFile, "{}");

        var job = factory.Services.GetServices<IHostedService>().OfType<ErrorLogRetentionJob>().Single();
        await job.RunOnceAsync(CancellationToken.None);

        var traces = await factory.WithDbAsync((db, _) =>
            db.ErrorLogs.AsNoTracking().Where(e => e.TraceId == oldTrace || e.TraceId == recentTrace).Select(e => e.TraceId).ToListAsync());
        Assert.Equal([recentTrace], traces);
        Assert.False(File.Exists(oldFile));
        Assert.True(File.Exists(recentFile));
    }
}
