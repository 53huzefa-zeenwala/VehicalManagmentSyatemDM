using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Auth;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Paging;
using VehicleManagementDM.Api.Common.Storage;
using VehicleManagementDM.Api.Common.Validation;
using VehicleManagementDM.Api.Data;
using VehicleManagementDM.Api.Domain.Entities;
using VehicleManagementDM.Api.Domain.Enums;

namespace VehicleManagementDM.Api.Features.Accounts;

/// <summary>
/// Staff and admin accounts (STF-01..02, AUTH-03..07). Records are deactivated, never deleted (BR-08).
/// Deactivation and password resets rotate the security stamp, which ends every session at once (AUTH-07).
/// </summary>
public sealed class AccountService(
    AppDbContext db,
    LoginIdGenerator loginIdGenerator,
    FileStore fileStore)
{
    private static readonly Expression<Func<User, StaffResponse>> ToStaff = u => new StaffResponse(
        u.Id,
        u.LoginId,
        u.FullName,
        u.Mobile,
        u.Email,
        u.LicenceNumber ?? string.Empty,
        u.PhotoFileName == null ? null : FileStore.Url(FileKind.StaffPhoto, u.PhotoFileName),
        u.IsActive,
        u.CreatedAtUtc,
        u.RowVersion);

    private static readonly Expression<Func<User, AdminResponse>> ToAdmin = u => new AdminResponse(
        u.Id, u.LoginId, u.FullName, u.Mobile, u.Email, u.IsActive, u.CreatedAtUtc, u.RowVersion);

    // ----- Staff -----

    public Task<PagedResult<StaffResponse>> ListStaffAsync(StaffQuery query, CancellationToken cancellationToken)
    {
        var staff = db.Users.AsNoTracking().Where(u => u.Role == UserRole.Staff);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var text = query.Search.Trim();
            staff = staff.Where(u => u.FullName.Contains(text) || u.LoginId.Contains(text) || u.Mobile.Contains(text));
        }

        if (query.IsActive is { } isActive)
        {
            staff = staff.Where(u => u.IsActive == isActive);
        }

        return staff.OrderBy(u => u.FullName).ThenBy(u => u.LoginId).Select(ToStaff).ToPageAsync(query, cancellationToken);
    }

    public async Task<StaffResponse> GetStaffAsync(int id, CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking().Where(u => u.Id == id && u.Role == UserRole.Staff).Select(ToStaff).SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Staff member not found.");

    public async Task<StaffResponse> CreateStaffAsync(CreateStaffRequest request, CancellationToken cancellationToken)
    {
        var user = new User { Role = UserRole.Staff };
        ApplyStaff(user, request);
        await CreateAsync(user, request.Password, cancellationToken);
        return await GetStaffAsync(user.Id, cancellationToken);
    }

    public async Task<StaffResponse> UpdateStaffAsync(int id, UpdateStaffRequest request, CancellationToken cancellationToken)
    {
        var user = await FindAsync(id, UserRole.Staff, cancellationToken);
        db.ExpectRowVersion(user, request.RowVersion);
        ApplyStaff(user, request);
        await db.SaveChangesAsync(cancellationToken);
        return await GetStaffAsync(id, cancellationToken);
    }

    // ----- Admins -----

    public async Task<IReadOnlyList<AdminResponse>> ListAdminsAsync(CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking().Where(u => u.Role == UserRole.Admin).OrderBy(u => u.FullName).Select(ToAdmin).ToListAsync(cancellationToken);

    public async Task<AdminResponse> GetAdminAsync(int id, CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking().Where(u => u.Id == id && u.Role == UserRole.Admin).Select(ToAdmin).SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Admin not found.");

    public async Task<AdminResponse> CreateAdminAsync(CreateAdminRequest request, CancellationToken cancellationToken)
    {
        var user = new User { Role = UserRole.Admin };
        ApplyAccount(user, request);
        await CreateAsync(user, request.Password, cancellationToken);
        return await GetAdminAsync(user.Id, cancellationToken);
    }

    public async Task<AdminResponse> UpdateAdminAsync(int id, UpdateAdminRequest request, CancellationToken cancellationToken)
    {
        var user = await FindAsync(id, UserRole.Admin, cancellationToken);
        db.ExpectRowVersion(user, request.RowVersion);
        ApplyAccount(user, request);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAdminAsync(id, cancellationToken);
    }

    // ----- Shared -----

    /// <summary>Deactivates or reactivates an account; deactivation ends its sessions at once (AUTH-07).</summary>
    public async Task SetStatusAsync(int id, UserRole role, AccountStatusRequest request, int actingUserId, CancellationToken cancellationToken)
    {
        var user = await FindAsync(id, role, cancellationToken);
        db.ExpectRowVersion(user, request.RowVersion);
        if (!request.IsActive && id == actingUserId)
        {
            throw new ConflictException(ErrorCodes.CannotDeactivateSelf, "Not allowed", "You can't deactivate your own account.");
        }

        if (user.IsActive != request.IsActive)
        {
            user.IsActive = request.IsActive;
            user.SecurityStamp = User.NewSecurityStamp();
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Sets a new password, unlocks the account and ends its sessions (AUTH-05, AUTH-07).</summary>
    public async Task ResetPasswordAsync(int id, UserRole role, string newPassword, CancellationToken cancellationToken)
    {
        var user = await FindAsync(id, role, cancellationToken);
        user.Password = newPassword;
        user.SecurityStamp = User.NewSecurityStamp();
        user.FailedLoginCount = 0;
        user.LockoutEndUtc = null;
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Stores a new staff photo (up to 20 MB, JPEG or PNG) and removes the old one (D-034).</summary>
    public async Task<StaffResponse> SetStaffPhotoAsync(int id, Stream content, CancellationToken cancellationToken)
    {
        var user = await FindAsync(id, UserRole.Staff, cancellationToken);
        var previous = user.PhotoFileName;
        await fileStore.SaveAsync(FileKind.StaffPhoto, content, async name =>
        {
            user.PhotoFileName = name;
            await db.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        if (previous is not null)
        {
            fileStore.Delete(FileKind.StaffPhoto, previous);
        }

        return await GetStaffAsync(id, cancellationToken);
    }

    private async Task<User> FindAsync(int id, UserRole role, CancellationToken cancellationToken) =>
        await db.Users.SingleOrDefaultAsync(u => u.Id == id && u.Role == role, cancellationToken)
        ?? throw new NotFoundException(role == UserRole.Staff ? "Staff member not found." : "Admin not found.");

    /// <summary>Issues the login ID and saves the account in one transaction (D-011, D-037).</summary>
    private async Task CreateAsync(User user, string password, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        user.LoginId = await loginIdGenerator.NextAsync(user.Role, cancellationToken);
        user.Password = password;
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static void ApplyAccount(User user, AccountRequest request)
    {
        user.FullName = Text.Clean(request.FullName);
        user.Mobile = request.Mobile.Trim();
        user.Email = Text.CleanOptional(request.Email)?.ToLowerInvariant();
    }

    private static void ApplyStaff(User user, StaffRequest request)
    {
        ApplyAccount(user, request);
        user.LicenceNumber = Text.Clean(request.LicenceNumber).ToUpperInvariant();
    }
}
