using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Auth;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Storage;
using VehicleManagementDM.Api.Data;

namespace VehicleManagementDM.Api.Controllers;

/// <summary>
/// Serves photos and signatures to signed-in users only (D-034, ARCHITECTURE.md §11).
/// Vehicle and staff photos: every signed-in user. Signatures: admins, and staff on trips they took part in.
/// </summary>
[ApiController]
[Route("api/v1/files")]
public sealed class FilesController(FileStore fileStore, AppDbContext db) : ControllerBase
{
    private const string ImmutableCache = "private, max-age=31536000, immutable";

    [HttpGet("{kind}/{name}")]
    public async Task<IActionResult> Get(string kind, string name, CancellationToken cancellationToken)
    {
        if (!FileStore.TryParseSegment(kind, out var fileKind) || !FileStore.IsValidName(name))
        {
            throw FileNotFound();
        }

        if (fileKind == FileKind.Signature && !User.IsInRole(Roles.Admin))
        {
            var staffId = User.GetUserId();
            var ownTrip = await db.TripSegments.AnyAsync(
                s => s.SignatureFileName == name && s.Trip.Segments.Any(x => x.StaffId == staffId),
                cancellationToken);
            if (!ownTrip)
            {
                throw new ForbiddenException("You can only view signatures on your own trips.");
            }
        }

        var file = fileStore.Open(fileKind, name) ?? throw FileNotFound();
        Response.Headers.CacheControl = ImmutableCache;
        return File(file.Content, file.ContentType);
    }

    private static NotFoundException FileNotFound() => new("The file was not found.", ErrorCodes.FileNotFound);
}
