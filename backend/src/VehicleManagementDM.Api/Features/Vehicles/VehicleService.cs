using System.Linq.Expressions;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Paging;
using VehicleManagementDM.Api.Common.Storage;
using VehicleManagementDM.Api.Common.Validation;
using VehicleManagementDM.Api.Data;
using VehicleManagementDM.Api.Domain.Entities;
using VehicleManagementDM.Api.Domain.Enums;
using VehicleManagementDM.Api.Features.Lots.Dtos;
using VehicleManagementDM.Api.Features.Trips;
using VehicleManagementDM.Api.Features.Vehicles.Dtos;

namespace VehicleManagementDM.Api.Features.Vehicles;

/// <summary>The vehicle catalogue (VEH-02..04, QR-01..04). Status Active is only ever set by trips and transfers.</summary>
public sealed class VehicleService(AppDbContext db, FileStore fileStore)
{
    public const int MaxQrLabels = 500;

    private static readonly Expression<Func<Vehicle, VehicleResponse>> ToResponse = v => new VehicleResponse(
        v.Id,
        v.RegistrationNumber,
        v.VehicleType.CategoryId,
        v.VehicleType.Category.Name,
        v.VehicleTypeId,
        v.VehicleType.Name,
        v.Brand,
        v.Model,
        v.Colour,
        v.PhotoFileName == null ? null : FileStore.Url(FileKind.VehiclePhoto, v.PhotoFileName),
        v.Status,
        v.CurrentLot == null ? null : new LotRef(v.CurrentLot.Id, v.CurrentLot.Code, v.CurrentLot.Name),
        v.CurrentLotId == null && v.Status != VehicleStatus.Active,
        v.QrToken,
        v.CreatedAtUtc,
        v.UpdatedAtUtc,
        v.RowVersion);

    /// <summary>Upper-case with spaces and dashes removed, so "d-01423" and "D 01423" are the same vehicle.</summary>
    public static string RegistrationKey(string registrationNumber) =>
        new(registrationNumber.Where(c => !char.IsWhiteSpace(c) && c != '-').Select(char.ToUpperInvariant).ToArray());

    public Task<PagedResult<VehicleResponse>> ListAsync(VehicleQuery query, CancellationToken cancellationToken)
    {
        var vehicles = db.Vehicles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var key = RegistrationKey(query.Search);
            var text = query.Search.Trim();
            vehicles = vehicles.Where(v => v.RegistrationKey.Contains(key) || v.Brand.Contains(text) || v.Model.Contains(text));
        }

        if (query.Status is { } status)
        {
            vehicles = vehicles.Where(v => v.Status == status);
        }

        if (query.CategoryId is { } categoryId)
        {
            vehicles = vehicles.Where(v => v.VehicleType.CategoryId == categoryId);
        }

        if (query.TypeId is { } typeId)
        {
            vehicles = vehicles.Where(v => v.VehicleTypeId == typeId);
        }

        if (query.LotId is { } lotId)
        {
            vehicles = vehicles.Where(v => v.CurrentLotId == lotId);
        }

        if (query.NotAtLot)
        {
            vehicles = vehicles.Where(v => v.CurrentLotId == null && v.Status != VehicleStatus.Active);
        }

        return vehicles.OrderBy(v => v.RegistrationKey).Select(ToResponse).ToPageAsync(query, cancellationToken);
    }

    public async Task<VehicleResponse> GetAsync(int id, CancellationToken cancellationToken) =>
        await db.Vehicles.AsNoTracking().Where(v => v.Id == id).Select(ToResponse).SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Vehicle not found.");

    public async Task<VehicleResponse> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken)
    {
        if (request.Status == VehicleStatus.Active)
        {
            throw BadRequestException.ForField("status", "A new vehicle can be Available, Inactive or Under maintenance.");
        }

        await EnsureUsableTypeAsync(request.VehicleTypeId, cancellationToken);
        if (request.LotId is { } lotId)
        {
            await EnsureActiveLotAsync(lotId, cancellationToken);
        }

        var vehicle = new Vehicle
        {
            QrToken = NewQrToken(),
            Status = request.Status,
            CurrentLotId = request.LotId,
        };
        Apply(vehicle, request);
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(vehicle.Id, cancellationToken);
    }

    public async Task<VehicleResponse> UpdateAsync(int id, UpdateVehicleRequest request, CancellationToken cancellationToken)
    {
        var vehicle = await FindAsync(id, cancellationToken);
        db.ExpectRowVersion(vehicle, request.RowVersion);
        if (vehicle.VehicleTypeId != request.VehicleTypeId)
        {
            await EnsureUsableTypeAsync(request.VehicleTypeId, cancellationToken);
        }

        Apply(vehicle, request);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Admins set Available, Inactive or Under maintenance, only while the vehicle isn't out (D-043 Q4).</summary>
    public async Task<VehicleResponse> SetStatusAsync(int id, VehicleStatusRequest request, CancellationToken cancellationToken)
    {
        var vehicle = await FindAsync(id, cancellationToken);
        db.ExpectRowVersion(vehicle, request.RowVersion);
        if (request.Status == VehicleStatus.Active)
        {
            throw BadRequestException.ForField("status", "Active is set automatically by trips and transfers.");
        }

        if (vehicle.Status == VehicleStatus.Active)
        {
            throw new ConflictException(
                ErrorCodes.StatusChangeNotAllowed,
                "Vehicle is out",
                $"{vehicle.RegistrationNumber} is on a trip or transfer. Change its status after it comes back.");
        }

        vehicle.Status = request.Status!.Value;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Places a vehicle at a lot, e.g. to resolve "Not at a lot" (VEH-04, D-012).</summary>
    public async Task<VehicleResponse> SetLocationAsync(int id, VehicleLocationRequest request, CancellationToken cancellationToken)
    {
        var vehicle = await FindAsync(id, cancellationToken);
        db.ExpectRowVersion(vehicle, request.RowVersion);
        if (vehicle.Status == VehicleStatus.Active)
        {
            throw new ConflictException(
                ErrorCodes.VehicleNotAvailable,
                "Vehicle is out",
                $"{vehicle.RegistrationNumber} is on a trip or transfer. Its location is set when it comes back.");
        }

        if (await db.HasOpenTransferAsync(vehicle.Id, cancellationToken))
        {
            throw new ConflictException(
                ErrorCodes.VehicleInTransfer,
                "Vehicle in transfer",
                $"{vehicle.RegistrationNumber} has an open transfer. Cancel it or let it arrive first.");
        }

        await EnsureActiveLotAsync(request.LotId, cancellationToken);
        vehicle.CurrentLotId = request.LotId;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Stores a new photo (up to 20 MB, JPEG or PNG) and removes the old one (D-034).</summary>
    public async Task<VehicleResponse> SetPhotoAsync(int id, Stream content, CancellationToken cancellationToken)
    {
        var vehicle = await FindAsync(id, cancellationToken);
        var previous = vehicle.PhotoFileName;
        await fileStore.SaveAsync(FileKind.VehiclePhoto, content, async name =>
        {
            vehicle.PhotoFileName = name;
            await db.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        if (previous is not null)
        {
            fileStore.Delete(FileKind.VehiclePhoto, previous);
        }

        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Label data for the app's QR image / PDF (QR-02, QR-03, D-040).</summary>
    public async Task<IReadOnlyList<QrLabelResponse>> QrLabelsAsync(int[] ids, CancellationToken cancellationToken)
    {
        if (ids.Length is 0 or > MaxQrLabels)
        {
            throw BadRequestException.ForField("ids", $"Pick between 1 and {MaxQrLabels} vehicles.");
        }

        var distinct = ids.Distinct().ToList();
        var labels = await db.Vehicles
            .AsNoTracking()
            .Where(v => distinct.Contains(v.Id))
            .OrderBy(v => v.RegistrationKey)
            .Select(v => new QrLabelResponse(v.Id, v.QrToken, v.RegistrationNumber, v.VehicleType.Category.Name, v.VehicleType.Name))
            .ToListAsync(cancellationToken);
        if (labels.Count != distinct.Count)
        {
            throw new NotFoundException("One or more of the vehicles were not found.");
        }

        return labels;
    }

    private async Task<Vehicle> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.Vehicles.SingleOrDefaultAsync(v => v.Id == id, cancellationToken)
        ?? throw new NotFoundException("Vehicle not found.");

    private static void Apply(Vehicle vehicle, VehicleRequest request)
    {
        var key = RegistrationKey(request.RegistrationNumber);
        if (key.Length == 0)
        {
            throw BadRequestException.ForField("registrationNumber", "Enter the registration number.");
        }

        vehicle.RegistrationNumber = Text.Clean(request.RegistrationNumber).ToUpperInvariant();
        vehicle.RegistrationKey = key;
        vehicle.VehicleTypeId = request.VehicleTypeId;
        vehicle.Brand = Text.Clean(request.Brand);
        vehicle.Model = Text.Clean(request.Model);
        vehicle.Colour = Text.Clean(request.Colour);
    }

    private async Task EnsureUsableTypeAsync(int typeId, CancellationToken cancellationToken)
    {
        var type = await db.VehicleTypes.AsNoTracking()
            .Where(t => t.Id == typeId)
            .Select(t => new { t.IsActive, CategoryActive = t.Category.IsActive })
            .SingleOrDefaultAsync(cancellationToken);
        if (type is null)
        {
            throw BadRequestException.ForField("vehicleTypeId", "This vehicle type doesn't exist.");
        }

        if (!type.IsActive || !type.CategoryActive)
        {
            throw BadRequestException.ForField("vehicleTypeId", "This vehicle type or its category is inactive.");
        }
    }

    private async Task EnsureActiveLotAsync(int lotId, CancellationToken cancellationToken)
    {
        var isActive = await db.ParkingLots.AsNoTracking().Where(l => l.Id == lotId).Select(l => (bool?)l.IsActive).SingleOrDefaultAsync(cancellationToken);
        if (isActive is null)
        {
            throw BadRequestException.ForField("lotId", "This parking lot doesn't exist.");
        }

        if (isActive == false)
        {
            throw BadRequestException.ForField("lotId", "This parking lot is inactive.");
        }
    }

    /// <summary>128 random bits, base64url: 22 characters, unguessable, no data inside (QR-01).</summary>
    private static string NewQrToken() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(16));
}
