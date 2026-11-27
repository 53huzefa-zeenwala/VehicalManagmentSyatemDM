using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Paging;
using VehicleManagementDM.Api.Data;
using VehicleManagementDM.Api.Domain.Entities;
using VehicleManagementDM.Api.Domain.Enums;

namespace VehicleManagementDM.Api.Features.Notifications;

/// <summary>
/// Sends phone push notifications (FCM/APNs). Until a Firebase project exists the API uses
/// <see cref="NoOpPushSender"/> and users see notifications in the in-app list only (D-039).
/// </summary>
public interface IPushSender
{
    Task SendAsync(IReadOnlyCollection<Notification> notifications, CancellationToken cancellationToken);
}

/// <summary>
/// The approved push setting until a Firebase project exists (D-039): nothing is sent to phones and the
/// in-app list is the only channel. Replace the registration in Program.cs with an FCM sender to enable push.
/// </summary>
public sealed class NoOpPushSender : IPushSender
{
    public Task SendAsync(IReadOnlyCollection<Notification> notifications, CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>In-app notifications (NTF-01..03). Rows are added to the caller's unit of work and saved with it.</summary>
public sealed class NotificationService(AppDbContext db, IPushSender pushSender, TimeProvider timeProvider)
{
    /// <summary>Adds one notification per recipient; call <see cref="PushAsync"/> after the save commits.</summary>
    public List<Notification> Add(IEnumerable<int> userIds, NotificationType type, string title, string body, string? targetType, int? targetId)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var rows = userIds.Distinct().Select(userId => new Notification
        {
            UserId = userId,
            Type = type,
            Title = title.Length > 120 ? title[..120] : title,
            Body = body.Length > 500 ? body[..500] : body,
            TargetType = targetType,
            TargetId = targetId,
            CreatedAtUtc = now,
        }).ToList();
        db.Notifications.AddRange(rows);
        return rows;
    }

    public Task PushAsync(IReadOnlyCollection<Notification> notifications, CancellationToken cancellationToken) =>
        notifications.Count == 0 ? Task.CompletedTask : pushSender.SendAsync(notifications, cancellationToken);

    public Task<List<int>> ActiveAdminIdsAsync(CancellationToken cancellationToken) =>
        db.Users.Where(u => u.Role == UserRole.Admin && u.IsActive).Select(u => u.Id).ToListAsync(cancellationToken);

    public Task<List<int>> ActiveStaffIdsAsync(CancellationToken cancellationToken) =>
        db.Users.Where(u => u.Role == UserRole.Staff && u.IsActive).Select(u => u.Id).ToListAsync(cancellationToken);

    public async Task<NotificationPage> ListAsync(int userId, bool unreadOnly, PageQuery page, CancellationToken cancellationToken)
    {
        var mine = db.Notifications.AsNoTracking().Where(n => n.UserId == userId);
        var unread = await mine.CountAsync(n => n.ReadAtUtc == null, cancellationToken);
        if (unreadOnly)
        {
            mine = mine.Where(n => n.ReadAtUtc == null);
        }

        var result = await mine
            .OrderByDescending(n => n.CreatedAtUtc).ThenByDescending(n => n.Id)
            .Select(n => new NotificationResponse(n.Id, n.Type, n.Title, n.Body, n.TargetType, n.TargetId, n.CreatedAtUtc, n.ReadAtUtc))
            .ToPageAsync(page, cancellationToken);
        return new NotificationPage(result.Items, result.Page, result.PageSize, result.TotalCount, unread);
    }

    public async Task MarkReadAsync(int userId, long id, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var updated = await db.Notifications
            .Where(n => n.Id == id && n.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAtUtc, n => n.ReadAtUtc ?? now), cancellationToken);
        if (updated == 0)
        {
            throw new Common.Errors.NotFoundException("Notification not found.");
        }
    }

    public Task MarkAllReadAsync(int userId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return db.Notifications
            .Where(n => n.UserId == userId && n.ReadAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAtUtc, now), cancellationToken);
    }
}

public sealed record NotificationResponse(
    long Id,
    NotificationType Type,
    string Title,
    string Body,
    string? TargetType,
    int? TargetId,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc);

public sealed record NotificationPage(IReadOnlyList<NotificationResponse> Items, int Page, int PageSize, int TotalCount, int UnreadCount);
