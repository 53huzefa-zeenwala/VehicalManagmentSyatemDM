using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Data;

namespace VehicleManagementDM.Api.Common.Database;

/// <summary>
/// SQL Server application locks that serialise actions on the same staff member, vehicle, trip or
/// transfer (ARCHITECTURE.md §13, M3). Must be called inside a transaction; released at commit or rollback.
/// </summary>
public static class AppLocks
{
    private const int TimeoutMilliseconds = 10_000;

    public static string Staff(int id) => $"vms:staff:{id}";

    public static string Vehicle(int id) => $"vms:vehicle:{id}";

    public static string Trip(int id) => $"vms:trip:{id}";

    public static string Transfer(int id) => $"vms:transfer:{id}";

    /// <summary>Takes exclusive locks, always in the same order so two actions can't deadlock.</summary>
    public static async Task LockAsync(this AppDbContext db, CancellationToken cancellationToken, params string[] resources)
    {
        foreach (var resource in resources.Distinct().Order(StringComparer.Ordinal))
        {
            var results = await db.Database
                .SqlQuery<int>($"DECLARE @result int; EXEC @result = sp_getapplock @Resource = {resource}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = {TimeoutMilliseconds}; SELECT @result AS [Value]")
                .ToListAsync(cancellationToken);
            if (results.Single() < 0)
            {
                throw new AppException(
                    StatusCodes.Status503ServiceUnavailable,
                    ErrorCodes.DatabaseTimeout,
                    "Busy",
                    "Another action on this vehicle or staff member is in progress. Try again.");
            }
        }
    }
}
