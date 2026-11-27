using Microsoft.AspNetCore.Mvc;
using VehicleManagementDM.Api.Common.Auth;
using VehicleManagementDM.Api.Common.Paging;
using VehicleManagementDM.Api.Features.Notifications;

namespace VehicleManagementDM.Api.Controllers;

/// <summary>The signed-in user's in-app notifications, newest first (NTF-03). No auto-refresh (D-035).</summary>
[ApiController]
[Route("api/v1/notifications")]
public sealed class NotificationsController(NotificationService notifications) : ControllerBase
{
    [HttpGet]
    public Task<NotificationPage> List([FromQuery] bool unreadOnly, [FromQuery] PageQuery page, CancellationToken cancellationToken) =>
        notifications.ListAsync(User.GetUserId(), unreadOnly, page, cancellationToken);

    [HttpPost("{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id, CancellationToken cancellationToken)
    {
        await notifications.MarkReadAsync(User.GetUserId(), id, cancellationToken);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await notifications.MarkAllReadAsync(User.GetUserId(), cancellationToken);
        return NoContent();
    }
}
