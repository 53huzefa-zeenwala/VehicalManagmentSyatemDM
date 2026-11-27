using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using VehicleManagementDM.Api.Common.Auth;
using VehicleManagementDM.Api.Domain.Entities;

namespace VehicleManagementDM.Api.Data.Interceptors;

/// <summary>
/// Records every change an admin makes to an <see cref="IAuditable"/> entity (AUD-01): who, when,
/// what, and the changed fields before and after. Audit rows are saved in the same transaction as
/// the change (a transaction is opened when the caller has none), so a change is never saved
/// without its audit entry. Secrets (password hashes, security stamps) are never written.
/// </summary>
public sealed class AuditInterceptor(IHttpContextAccessor httpContextAccessor, TimeProvider timeProvider) : SaveChangesInterceptor
{
    private static readonly HashSet<string> HiddenProperties =
    [
        nameof(User.PasswordHash), nameof(User.SecurityStamp), nameof(User.RowVersion), nameof(User.FailedLoginCount),
        nameof(User.LockoutEndUtc), nameof(IHasTimestamps.CreatedAtUtc), nameof(IHasTimestamps.UpdatedAtUtc),
    ];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ConditionalWeakTable<DbContext, PendingAudit> _pending = new();

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (Prepare(eventData.Context) is { } pending && eventData.Context!.Database.CurrentTransaction is null)
        {
            pending.OwnedTransaction = await eventData.Context.Database.BeginTransactionAsync(cancellationToken);
        }

        return result;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (Prepare(eventData.Context) is { } pending && eventData.Context!.Database.CurrentTransaction is null)
        {
            pending.OwnedTransaction = eventData.Context.Database.BeginTransaction();
        }

        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null || !_pending.TryGetValue(context, out var pending) || pending.Writing)
        {
            return result;
        }

        try
        {
            pending.Writing = true;
            AddAuditRows(context, pending);
            await context.SaveChangesAsync(cancellationToken);
            if (pending.OwnedTransaction is not null)
            {
                await pending.OwnedTransaction.CommitAsync(cancellationToken);
            }
        }
        finally
        {
            await EndAsync(context, pending);
        }

        return result;
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        var context = eventData.Context;
        if (context is null || !_pending.TryGetValue(context, out var pending) || pending.Writing)
        {
            return result;
        }

        try
        {
            pending.Writing = true;
            AddAuditRows(context, pending);
            context.SaveChanges();
            pending.OwnedTransaction?.Commit();
        }
        finally
        {
            EndAsync(context, pending).AsTask().GetAwaiter().GetResult();
        }

        return result;
    }

    public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context && _pending.TryGetValue(context, out var pending) && !pending.Writing)
        {
            await EndAsync(context, pending);
        }
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        if (eventData.Context is { } context && _pending.TryGetValue(context, out var pending) && !pending.Writing)
        {
            EndAsync(context, pending).AsTask().GetAwaiter().GetResult();
        }
    }

    private PendingAudit? Prepare(DbContext? context)
    {
        if (context is null || _pending.TryGetValue(context, out _))
        {
            return null; // nothing to do, or this is the save of the audit rows themselves
        }

        var user = httpContextAccessor.HttpContext?.User;
        if (user is null || !user.IsInRole(Roles.Admin))
        {
            return null; // AUD-01 covers admin actions
        }

        var adminId = user.GetUserId();
        var items = context.ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditable && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => Describe(e, adminId))
            .OfType<PendingItem>()
            .ToList();
        if (items.Count == 0)
        {
            return null;
        }

        var pending = new PendingAudit(adminId, items);
        _pending.Add(context, pending);
        return pending;
    }

    private static PendingItem? Describe(EntityEntry entry, int adminId)
    {
        var entityType = entry.Metadata.ClrType.Name;
        var compositeKey = entry.Metadata.FindPrimaryKey()!.Properties.Count > 1;

        switch (entry.State)
        {
            case EntityState.Added:
                return new PendingItem(entry, "Create", entityType, null, null, null); // values (and key) taken after saving

            case EntityState.Deleted:
                return new PendingItem(entry, "Delete", entityType, Snapshot(entry, current: false, changedOnly: false, compositeKey), null, KeyOf(entry));
        }

        var changed = entry.Properties
            .Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue))
            .Select(p => p.Metadata.Name)
            .ToHashSet();
        var visibleChanges = changed.Where(name => !HiddenProperties.Contains(name)).ToList();

        string action;
        if (visibleChanges.Contains(nameof(User.IsActive)))
        {
            action = (bool)entry.Property(nameof(User.IsActive)).CurrentValue! ? "Reactivate" : "Deactivate";
        }
        else if (entry.Entity is Vehicle && visibleChanges.Contains(nameof(Vehicle.Status)))
        {
            action = "StatusChange";
        }
        else if (entry.Entity is Vehicle && visibleChanges.Contains(nameof(Vehicle.CurrentLotId)))
        {
            action = "LocationResolve";
        }
        else if (visibleChanges.Count > 0)
        {
            action = "Update";
        }
        else if (changed.Contains(nameof(User.PasswordHash)))
        {
            action = KeyOf(entry) == adminId ? "PasswordChange" : "PasswordReset";
        }
        else
        {
            return null; // only bookkeeping fields changed
        }

        return new PendingItem(
            entry,
            action,
            entityType,
            Snapshot(entry, current: false, changedOnly: true, compositeKey),
            Snapshot(entry, current: true, changedOnly: true, compositeKey),
            KeyOf(entry));
    }

    private static string? Snapshot(EntityEntry entry, bool current, bool changedOnly, bool includeKey)
    {
        var values = new Dictionary<string, object?>();
        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;
            var isKey = property.Metadata.IsPrimaryKey();
            if (HiddenProperties.Contains(name))
            {
                continue;
            }

            var include = !changedOnly
                || (property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
                || (includeKey && isKey);
            if (include)
            {
                values[JsonNamingPolicy.CamelCase.ConvertName(name)] = current ? property.CurrentValue : property.OriginalValue;
            }
        }

        return values.Count == 0 ? null : JsonSerializer.Serialize(values, JsonOptions);
    }

    private static int KeyOf(EntityEntry entry)
    {
        var keyProperty = entry.Metadata.FindPrimaryKey()!.Properties[0];
        return Convert.ToInt32(entry.Property(keyProperty.Name).CurrentValue, System.Globalization.CultureInfo.InvariantCulture);
    }

    private void AddAuditRows(DbContext context, PendingAudit pending)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var item in pending.Items)
        {
            context.Add(new AuditLog
            {
                AtUtc = now,
                UserId = pending.AdminId,
                Action = item.Action,
                EntityType = item.EntityType,
                EntityId = item.KnownId ?? KeyOf(item.Entry), // keys of new rows are known only after saving
                BeforeJson = item.BeforeJson,
                AfterJson = item.AfterJson ?? (item.Action == "Create" ? Snapshot(item.Entry, current: true, changedOnly: false, includeKey: true) : null),
            });
        }
    }

    private async ValueTask EndAsync(DbContext context, PendingAudit pending)
    {
        _pending.Remove(context);
        if (pending.OwnedTransaction is not null)
        {
            await pending.OwnedTransaction.DisposeAsync(); // rolls back if not committed
        }
    }

    private sealed class PendingAudit(int adminId, List<PendingItem> items)
    {
        public int AdminId { get; } = adminId;

        public List<PendingItem> Items { get; } = items;

        public IDbContextTransaction? OwnedTransaction { get; set; }

        public bool Writing { get; set; }
    }

    private sealed record PendingItem(EntityEntry Entry, string Action, string EntityType, string? BeforeJson, string? AfterJson, int? KnownId);
}
