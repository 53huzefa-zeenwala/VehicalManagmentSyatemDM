using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleManagementDM.Api.Common.Auth;
using VehicleManagementDM.Api.Common.Paging;
using VehicleManagementDM.Api.Features.Trips;
using VehicleManagementDM.Api.Features.Trips.Dtos;

namespace VehicleManagementDM.Api.Controllers;

/// <summary>
/// Trips (TRP). Staff start, swap, hand over, cancel and end their own trips; admins only monitor (D-021).
/// </summary>
[ApiController]
[Route("api/v1/trips")]
public sealed class TripsController(TripService trips, TripQueries queries) : ControllerBase
{
    /// <summary>Scanned QR code or typed registration → vehicle card and whether it can be started (QR-05, QR-06).</summary>
    [HttpGet("/api/v1/vehicles/lookup")]
    [Authorize(Roles = Roles.Staff)]
    public Task<VehicleLookupResponse> Lookup([FromQuery] VehicleLookupQuery query, CancellationToken cancellationToken) =>
        trips.LookupAsync(User.GetUserId(), query, cancellationToken);

    /// <summary>Starts a trip (TRP-01..03).</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Staff)]
    public async Task<ActionResult<TripDetailResponse>> Start(StartTripRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await trips.StartAsync(User.GetUserId(), request, cancellationToken));

    /// <summary>The caller's running trip, or 204 when there is none (TRP-04).</summary>
    [HttpGet("active")]
    [Authorize(Roles = Roles.Staff)]
    public async Task<ActionResult<TripDetailResponse>> Active(CancellationToken cancellationToken)
    {
        var trip = await queries.GetActiveForStaffAsync(User.GetUserId(), cancellationToken);
        return trip is null ? NoContent() : Ok(trip);
    }

    /// <summary>Trip history (TRP-12): staff get trips they took part in; admins get all, with filters.</summary>
    [HttpGet]
    public Task<PagedResult<TripSummaryResponse>> List([FromQuery] TripQuery query, CancellationToken cancellationToken) =>
        queries.ListAsync(query, User.GetUserId(), User.IsInRole(Roles.Admin), cancellationToken);

    /// <summary>Active trips, overdue first, for the admin Live tab (TRP-05).</summary>
    [HttpGet("live")]
    [Authorize(Roles = Roles.Admin)]
    public Task<IReadOnlyList<TripSummaryResponse>> Live([FromQuery] LiveTripsQuery query, CancellationToken cancellationToken) =>
        queries.LiveAsync(query, cancellationToken);

    /// <summary>Trip detail with segments (TRP-10). Staff only see trips they took part in.</summary>
    [HttpGet("{id:int}")]
    public Task<TripDetailResponse> Get(int id, CancellationToken cancellationToken) =>
        queries.GetDetailAsync(id, User.GetUserId(), User.IsInRole(Roles.Admin), null, cancellationToken);

    [HttpPost("{id:int}/swap-vehicle")]
    [Authorize(Roles = Roles.Staff)]
    public Task<TripDetailResponse> SwapVehicle(int id, SwapVehicleRequest request, CancellationToken cancellationToken) =>
        trips.SwapVehicleAsync(User.GetUserId(), id, request, cancellationToken);

    [HttpPost("{id:int}/handover")]
    [Authorize(Roles = Roles.Staff)]
    public Task<TripDetailResponse> HandOver(int id, HandOverRequest request, CancellationToken cancellationToken) =>
        trips.HandOverAsync(User.GetUserId(), id, request, cancellationToken);

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = Roles.Staff)]
    public Task<TripDetailResponse> Cancel(int id, CancelTripRequest request, CancellationToken cancellationToken) =>
        trips.CancelAsync(User.GetUserId(), id, request, cancellationToken);

    /// <summary>Ends the trip; the response is the trip summary, with lot-capacity warnings if any (TRP-09, TRP-10).</summary>
    [HttpPost("{id:int}/end")]
    [Authorize(Roles = Roles.Staff)]
    public Task<TripDetailResponse> End(int id, EndTripRequest request, CancellationToken cancellationToken) =>
        trips.EndAsync(User.GetUserId(), id, request, cancellationToken);

    /// <summary>Staff who can take over a trip: active, not the caller, no active trip (TRP-07).</summary>
    [HttpGet("/api/v1/staff/handover-candidates")]
    [Authorize(Roles = Roles.Staff)]
    public Task<IReadOnlyList<StaffRef>> HandoverCandidates([FromQuery] string? search, CancellationToken cancellationToken) =>
        queries.HandoverCandidatesAsync(User.GetUserId(), search, cancellationToken);

    /// <summary>A staff member's trips and hours in a period (STF-03).</summary>
    [HttpGet("/api/v1/staff/{staffId:int}/trips")]
    [Authorize(Roles = Roles.Admin)]
    public Task<StaffTripsResponse> StaffTrips(int staffId, [FromQuery] HoursQuery query, CancellationToken cancellationToken) =>
        queries.StaffTripsAsync(staffId, query, cancellationToken);
}
