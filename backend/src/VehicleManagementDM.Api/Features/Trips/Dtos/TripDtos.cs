using System.ComponentModel.DataAnnotations;
using VehicleManagementDM.Api.Common.Paging;
using VehicleManagementDM.Api.Domain.Enums;
using VehicleManagementDM.Api.Features.Lots.Dtos;

namespace VehicleManagementDM.Api.Features.Trips.Dtos;

// ----- Requests -----

/// <summary>A vehicle is identified by its scanned QR token or, if the scan fails, its typed registration number (QR-05).</summary>
public abstract class VehicleIdentification : IValidatableObject
{
    [StringLength(22, ErrorMessage = "The QR code is not valid.")]
    public string? VehicleQrToken { get; init; }

    [StringLength(30, ErrorMessage = "The registration number can have at most 30 characters.")]
    public string? VehicleRegistration { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(VehicleQrToken) == string.IsNullOrWhiteSpace(VehicleRegistration))
        {
            yield return new ValidationResult("Scan the vehicle's QR code or type its registration number.", ["vehicleQrToken"]);
        }
    }
}

/// <summary>The origin lot is the active lot the vehicle is parked at; staff have no assigned lots (D-047).</summary>
public sealed class StartTripRequest : VehicleIdentification
{
    [Range(1, int.MaxValue, ErrorMessage = "Pick a destination.")]
    public int DestinationLocationId { get; init; }

    /// <summary>The staff member's signature as a base64 PNG (a "data:image/png;base64," prefix is allowed).</summary>
    [Required(ErrorMessage = "Sign to confirm taking the vehicle.")]
    public string SignaturePngBase64 { get; init; } = string.Empty;
}

public sealed class SwapVehicleRequest : IValidatableObject
{
    [StringLength(22, ErrorMessage = "The QR code is not valid.")]
    public string? NewVehicleQrToken { get; init; }

    [StringLength(30, ErrorMessage = "The registration number can have at most 30 characters.")]
    public string? NewVehicleRegistration { get; init; }

    [Required(ErrorMessage = "Enter the reason for the swap.")]
    [StringLength(500, ErrorMessage = "The reason can have at most 500 characters.")]
    public string Reason { get; init; } = string.Empty;

    /// <summary>Where the previous vehicle is left; null = "Not at a lot" (D-012).</summary>
    public int? OldVehicleLotId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(NewVehicleQrToken) == string.IsNullOrWhiteSpace(NewVehicleRegistration))
        {
            yield return new ValidationResult("Scan the new vehicle's QR code or type its registration number.", ["newVehicleQrToken"]);
        }
    }
}

public sealed class HandOverRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Pick the staff member taking over.")]
    public int ToStaffId { get; init; }

    [Required(ErrorMessage = "Enter the reason for the hand-over.")]
    [StringLength(500, ErrorMessage = "The reason can have at most 500 characters.")]
    public string Reason { get; init; } = string.Empty;
}

public sealed class CancelTripRequest
{
    [Required(ErrorMessage = "Pick a reason.")]
    public CancelCategory? Category { get; init; }

    [Required(ErrorMessage = "Add a note.")]
    [StringLength(500, ErrorMessage = "The note can have at most 500 characters.")]
    public string Note { get; init; } = string.Empty;

    /// <summary>Where the vehicle is now; null = "Not at a lot".</summary>
    public int? VehicleLotId { get; init; }
}

public sealed class EndTripRequest : VehicleIdentification
{
    [Range(1, int.MaxValue, ErrorMessage = "Pick the lot you returned the vehicle to.")]
    public int ReturnLotId { get; init; }
}

public sealed class VehicleLookupQuery : IValidatableObject
{
    [StringLength(22)]
    public string? Qr { get; init; }

    [StringLength(30)]
    public string? Registration { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Qr) == string.IsNullOrWhiteSpace(Registration))
        {
            yield return new ValidationResult("Send either qr or registration.", ["qr"]);
        }
    }
}

public sealed class TripQuery : PageQuery
{
    /// <summary>First GST day (inclusive) of the trip start.</summary>
    public DateOnly? From { get; init; }

    /// <summary>Last GST day (inclusive) of the trip start.</summary>
    public DateOnly? To { get; init; }

    public TripStatus? Status { get; init; }

    /// <summary>Admins only: trips this staff member took part in.</summary>
    public int? StaffId { get; init; }

    /// <summary>Trips this vehicle took part in.</summary>
    public int? VehicleId { get; init; }

    /// <summary>Origin lot.</summary>
    public int? LotId { get; init; }
}

public sealed class LiveTripsQuery
{
    /// <summary>Origin lot.</summary>
    public int? LotId { get; init; }

    /// <summary>Type of the vehicle now on the trip.</summary>
    public int? TypeId { get; init; }
}

public sealed class HoursQuery : PageQuery
{
    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }
}

// ----- Responses -----

public sealed record StaffRef(int Id, string LoginId, string FullName, string? PhotoUrl);

public sealed record VehicleRef(int Id, string RegistrationNumber, string Category, string Type);

public sealed record VehicleCardResponse(
    int Id,
    string RegistrationNumber,
    string Category,
    string Type,
    string Brand,
    string Model,
    string Colour,
    string? PhotoUrl,
    VehicleStatus Status,
    LotRef? CurrentLot);

public sealed record Notice(string Code, string Message);

/// <summary>Result of scanning or typing a vehicle (QR-05, QR-06).</summary>
public sealed record VehicleLookupResponse(VehicleCardResponse Vehicle, bool CanStart, Notice? BlockReason);

public sealed record DestinationRef(int LocationId, string Area, string Location);

public sealed record CancelInfo(CancelCategory Category, string Note);

public sealed record SegmentResponse(
    int Seq,
    SegmentKind Kind,
    StaffRef Staff,
    VehicleRef Vehicle,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    string? Reason,
    LotRef? OutgoingVehicleLot,
    bool OutgoingVehicleNotAtLot);

/// <summary>Full trip (TRP-10 summary, S-09 active trip, A-04 admin detail).</summary>
public sealed record TripDetailResponse(
    int Id,
    TripStatus Status,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    long? TotalSeconds,
    bool IsOverdue,
    DateTime? NextReminderAtUtc,
    LotRef OriginLot,
    DestinationRef Destination,
    StaffRef CurrentStaff,
    VehicleCardResponse CurrentVehicle,
    string? SignatureUrl,
    IReadOnlyList<SegmentResponse> Segments,
    LotRef? ReturnLot,
    CancelInfo? Cancel,
    IReadOnlyList<Notice> Warnings,
    IReadOnlyList<ReminderEntry> Reminders);

/// <summary>One reminder sent for a trip or transfer (TRP-06); shown to admins (A-04).</summary>
public sealed record ReminderEntry(int Number, DateTime SentAtUtc);

/// <summary>One row of a trip list (S-16, A-02, R-4).</summary>
public sealed record TripSummaryResponse(
    int Id,
    TripStatus Status,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    long? TotalSeconds,
    bool IsOverdue,
    LotRef OriginLot,
    LotRef? ReturnLot,
    DestinationRef Destination,
    StaffRef CurrentStaff,
    VehicleRef CurrentVehicle,
    int SegmentCount);

/// <summary>A staff member's trips and hours (STF-03). Hours are the staff member's segment time within the range (BR-06).</summary>
public sealed record StaffTripsResponse(StaffRef Staff, long TotalSeconds, PagedResult<TripSummaryResponse> Trips);

/// <summary>"My trips this month" on Profile and Home (GST calendar month).</summary>
public sealed record MonthStats(int Trips, long TotalSeconds);
