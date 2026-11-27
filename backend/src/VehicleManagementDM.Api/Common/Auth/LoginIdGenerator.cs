using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Time;
using VehicleManagementDM.Api.Data;
using VehicleManagementDM.Api.Domain.Entities;
using VehicleManagementDM.Api.Domain.Enums;

namespace VehicleManagementDM.Api.Common.Auth;

/// <summary>
/// Issues login IDs: prefix + 2-digit GST year + 5-digit sequence, e.g. DM2600042 / ADM2600001
/// (D-011, D-037). The sequence is incremented atomically in the database and never reused.
/// </summary>
public sealed class LoginIdGenerator(AppDbContext db, TimeProvider timeProvider)
{
    public const string AdminPrefix = "ADM";
    private const int MaxNumber = 99_999;

    public async Task<string> NextAsync(UserRole role, CancellationToken cancellationToken)
    {
        var prefix = role == UserRole.Admin
            ? AdminPrefix
            : await db.AppSettings
                .AsNoTracking()
                .Where(s => s.Id == AppSetting.SingletonId)
                .Select(s => s.StaffIdPrefix)
                .SingleAsync(cancellationToken);

        var roleName = role.ToString();
        var numbers = await db.Database
            .SqlQuery<int>($"UPDATE [IdSequences] SET [LastNumber] = [LastNumber] + 1 OUTPUT inserted.[LastNumber] AS [Value] WHERE [Role] = {roleName}")
            .ToListAsync(cancellationToken);

        var number = numbers.Single();
        if (number > MaxNumber)
        {
            throw new ConflictException(
                ErrorCodes.IdSequenceExhausted,
                "No more IDs",
                $"All {MaxNumber:N0} {roleName.ToLowerInvariant()} IDs have been used. Contact support.");
        }

        var year = Gst.Now(timeProvider).Year % 100;
        return $"{prefix}{year:00}{number:00000}";
    }
}
