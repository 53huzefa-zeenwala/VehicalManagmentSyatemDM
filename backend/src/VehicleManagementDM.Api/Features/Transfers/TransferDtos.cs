using System.ComponentModel.DataAnnotations;
using VehicleManagementDM.Api.Common.Paging;
using VehicleManagementDM.Api.Domain.Enums;
using VehicleManagementDM.Api.Features.Lots.Dtos;
using VehicleManagementDM.Api.Features.Trips.Dtos;

namespace VehicleManagementDM.Api.Features.Transfers;

public sealed class CreateTransferRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Pick a vehicle.")]
    public int VehicleId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Pick the destination lot.")]
    public int ToLotId { get; init; }

    [StringLength(500, ErrorMessage = "The note can have at most 500 characters.")]
    public string? Note { get; init; }
}

public sealed class TransferQuery : PageQuery
{
    public TransferStatus? Status { get; init; }

    /// <summary>Transfers from or to this lot.</summary>
    public int? LotId { get; init; }

    public int? VehicleId { get; init; }

    /// <summary>First GST day (inclusive) the transfer was created.</summary>
    public DateOnly? From { get; init; }

    /// <summary>Last GST day (inclusive) the transfer was created.</summary>
    public DateOnly? To { get; init; }
}

/// <summary>A transfer with its timeline (A-07, S-22). The driver is whoever confirmed departure (D-020).</summary>
public sealed record TransferResponse(
    int Id,
    TransferStatus Status,
    VehicleRef Vehicle,
    LotRef FromLot,
    LotRef ToLot,
    string? Note,
    StaffRef CreatedBy,
    DateTime CreatedAtUtc,
    StaffRef? Driver,
    DateTime? DepartedAtUtc,
    StaffRef? ArrivedBy,
    DateTime? ArrivedAtUtc,
    StaffRef? CancelledBy,
    DateTime? CancelledAtUtc,
    long? DrivingSeconds,
    bool IsOverdue,
    IReadOnlyList<Notice> Warnings);

/// <summary>What staff can act on: transfers waiting for someone to drive them, and transfers on the road.</summary>
public sealed record TransferTasksResponse(IReadOnlyList<TransferResponse> AwaitingDeparture, IReadOnlyList<TransferResponse> InTransit);
