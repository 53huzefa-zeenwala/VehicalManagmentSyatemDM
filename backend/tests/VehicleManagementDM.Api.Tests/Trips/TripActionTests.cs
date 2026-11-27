using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Paging;
using VehicleManagementDM.Api.Domain.Enums;
using VehicleManagementDM.Api.Features.Trips.Dtos;
using VehicleManagementDM.Api.Tests.Infrastructure;

namespace VehicleManagementDM.Api.Tests.Trips;

/// <summary>Swap, hand-over, cancel, end, history, live view and hours (TRP-05..12, BR-06, BR-07, BR-09, D-043).</summary>
[Collection(ApiCollection.Name)]
public sealed class TripActionTests(ApiFactory factory)
{
    [Fact]
    public async Task Vehicle_swap_continues_the_trip_in_a_new_segment()
    {
        var world = await TripWorld.CreateAsync(factory);
        var trip = await world.StartedTripAsync();
        var replacement = await world.AnotherVehicleAsync();

        var swapped = await ApiAssert.OkAsync<TripDetailResponse>(await world.Staff.PostAsJsonAsync(
            $"/api/v1/trips/{trip.Id}/swap-vehicle",
            new { newVehicleQrToken = replacement.QrToken, reason = "Flat tyre", oldVehicleLotId = world.Lot.Id }));

        Assert.Equal(trip.Id, swapped.Id);
        Assert.Equal(replacement.Id, swapped.CurrentVehicle.Id);
        Assert.Equal([SegmentKind.Start, SegmentKind.VehicleSwap], swapped.Segments.Select(s => s.Kind));
        Assert.NotNull(swapped.Segments[0].EndedAtUtc);
        Assert.Equal(("Flat tyre", world.Lot.Id), (swapped.Segments[1].Reason, swapped.Segments[1].OutgoingVehicleLot!.Id));

        var oldVehicle = await world.AdminVehicleAsync(world.Vehicle.Id);
        Assert.Equal((VehicleStatus.Available, world.Lot.Id), (oldVehicle.Status, oldVehicle.CurrentLot!.Id));
        Assert.Equal(VehicleStatus.Active, (await world.AdminVehicleAsync(replacement.Id)).Status);

        var sameAgain = await world.Staff.PostAsJsonAsync(
            $"/api/v1/trips/{trip.Id}/swap-vehicle", new { newVehicleQrToken = replacement.QrToken, reason = "x" });
        await ApiAssert.ErrorAsync(sameAgain, HttpStatusCode.BadRequest, ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task Hand_over_moves_the_trip_to_another_staff_member()
    {
        var world = await TripWorld.CreateAsync(factory);
        var trip = await world.StartedTripAsync();
        var (busyUser, busy) = await world.AnotherStaffAsync();
        await world.StartedTripAsync(busy, await world.AnotherVehicleAsync());
        var (nextUser, next) = await world.AnotherStaffAsync();

        var candidates = await ApiAssert.OkAsync<List<StaffRef>>(await world.Staff.GetAsync("/api/v1/staff/handover-candidates"));
        var toBusy = await world.Staff.PostAsJsonAsync($"/api/v1/trips/{trip.Id}/handover", new { toStaffId = busyUser.Id, reason = "Shift change" });
        var handed = await ApiAssert.OkAsync<TripDetailResponse>(await world.Staff.PostAsJsonAsync(
            $"/api/v1/trips/{trip.Id}/handover", new { toStaffId = nextUser.Id, reason = "Shift change" }));

        Assert.Contains(candidates, c => c.Id == nextUser.Id);
        Assert.DoesNotContain(candidates, c => c.Id == busyUser.Id || c.Id == world.StaffUser.Id);
        await ApiAssert.ErrorAsync(toBusy, HttpStatusCode.Conflict, ErrorCodes.HandoverTargetInvalid);
        Assert.Equal(nextUser.Id, handed.CurrentStaff.Id);
        Assert.Equal(SegmentKind.HandOver, handed.Segments[^1].Kind);

        var oldDriverEnds = await world.Staff.PostAsJsonAsync(
            $"/api/v1/trips/{trip.Id}/end", new { vehicleQrToken = world.Vehicle.QrToken, returnLotId = world.Lot.Id });
        await ApiAssert.ErrorAsync(oldDriverEnds, HttpStatusCode.Forbidden, ErrorCodes.NotYourTrip);
        Assert.Equal(trip.Id, (await ApiAssert.OkAsync<TripDetailResponse>(await next.GetAsync("/api/v1/trips/active"))).Id);
        Assert.Equal(HttpStatusCode.NoContent, (await world.Staff.GetAsync("/api/v1/trips/active")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await world.Staff.GetAsync($"/api/v1/trips/{trip.Id}")).StatusCode); // still in their history
    }

    [Fact]
    public async Task Cancel_ends_the_trip_now_and_leaves_the_vehicle_where_staff_say()
    {
        var world = await TripWorld.CreateAsync(factory);
        var trip = await world.StartedTripAsync();

        var cancelled = await ApiAssert.OkAsync<TripDetailResponse>(await world.Staff.PostAsJsonAsync(
            $"/api/v1/trips/{trip.Id}/cancel", new { category = "Technical", note = "Engine warning light", vehicleLotId = (int?)null }));

        Assert.Equal(TripStatus.Cancelled, cancelled.Status);
        Assert.Equal(new CancelInfo(CancelCategory.Technical, "Engine warning light"), cancelled.Cancel);
        Assert.NotNull(cancelled.TotalSeconds);
        var vehicle = await world.AdminVehicleAsync(world.Vehicle.Id);
        Assert.Equal((VehicleStatus.Available, true), (vehicle.Status, vehicle.IsNotAtLot));
    }

    [Fact]
    public async Task End_checks_the_vehicle_and_the_lot_then_returns_the_summary()
    {
        var world = await TripWorld.CreateAsync(factory);
        var trip = await world.StartedTripAsync();
        var other = await world.AnotherVehicleAsync();
        var closedLot = await world.Admin.CreateLotAsync();
        (await world.Admin.PutAsJsonAsync($"/api/v1/lots/{closedLot.Id}/status", new { isActive = false, rowVersion = closedLot.RowVersion })).EnsureSuccessStatusCode();
        var returnLot = await world.Admin.CreateLotAsync();

        var wrongVehicle = await world.Staff.PostAsJsonAsync($"/api/v1/trips/{trip.Id}/end", new { vehicleQrToken = other.QrToken, returnLotId = world.Lot.Id });
        var inactiveLot = await world.Staff.PostAsJsonAsync($"/api/v1/trips/{trip.Id}/end", new { vehicleQrToken = world.Vehicle.QrToken, returnLotId = closedLot.Id });
        var ended = await ApiAssert.OkAsync<TripDetailResponse>(await world.Staff.PostAsJsonAsync(
            $"/api/v1/trips/{trip.Id}/end", new { vehicleRegistration = world.Vehicle.RegistrationNumber, returnLotId = returnLot.Id }));
        var again = await world.Staff.PostAsJsonAsync($"/api/v1/trips/{trip.Id}/end", new { vehicleQrToken = world.Vehicle.QrToken, returnLotId = returnLot.Id });

        await ApiAssert.ErrorAsync(wrongVehicle, HttpStatusCode.Conflict, ErrorCodes.VehicleMismatch);
        await ApiAssert.ErrorAsync(inactiveLot, HttpStatusCode.Conflict, ErrorCodes.LotInactive);
        Assert.Equal(TripStatus.Ended, ended.Status);
        Assert.Equal(returnLot.Id, ended.ReturnLot!.Id);
        Assert.True(ended.TotalSeconds >= 0);
        Assert.Empty(ended.Warnings);
        Assert.All(ended.Segments, s => Assert.NotNull(s.EndedAtUtc));
        await ApiAssert.ErrorAsync(again, HttpStatusCode.Conflict, ErrorCodes.TripNotActive);

        var vehicle = await world.AdminVehicleAsync(world.Vehicle.Id);
        Assert.Equal((VehicleStatus.Available, returnLot.Id), (vehicle.Status, vehicle.CurrentLot!.Id));
    }

    [Fact]
    public async Task Returning_to_a_full_lot_is_allowed_with_a_warning()
    {
        var world = await TripWorld.CreateAsync(factory);
        var trip = await world.StartedTripAsync();
        var fullLot = await world.Admin.CreateLotAsync(totalCapacity: 0);

        var ended = await ApiAssert.OkAsync<TripDetailResponse>(await world.Staff.PostAsJsonAsync(
            $"/api/v1/trips/{trip.Id}/end", new { vehicleQrToken = world.Vehicle.QrToken, returnLotId = fullLot.Id }));

        Assert.Equal(TripStatus.Ended, ended.Status);
        Assert.Equal(ErrorCodes.LotFull, Assert.Single(ended.Warnings).Code);
    }

    [Fact]
    public async Task Staff_see_only_their_own_trips_admins_see_all()
    {
        var world = await TripWorld.CreateAsync(factory);
        var mine = await world.StartedTripAsync();
        var (otherUser, other) = await world.AnotherStaffAsync();
        var theirs = await world.StartedTripAsync(other, await world.AnotherVehicleAsync());

        var myList = await ApiAssert.OkAsync<PagedResult<TripSummaryResponse>>(await world.Staff.GetAsync("/api/v1/trips"));
        var adminList = await ApiAssert.OkAsync<PagedResult<TripSummaryResponse>>(await world.Admin.GetAsync($"/api/v1/trips?staffId={otherUser.Id}"));

        Assert.Equal([mine.Id], myList.Items.Select(t => t.Id));
        Assert.Equal([theirs.Id], adminList.Items.Select(t => t.Id));
        await ApiAssert.ErrorAsync(await world.Staff.GetAsync($"/api/v1/trips/{theirs.Id}"), HttpStatusCode.NotFound, ErrorCodes.NotFound);
        Assert.Equal(HttpStatusCode.OK, (await world.Admin.GetAsync($"/api/v1/trips/{theirs.Id}")).StatusCode);
    }

    [Fact]
    public async Task Live_view_lists_overdue_trips_first_and_is_for_admins()
    {
        var world = await TripWorld.CreateAsync(factory);
        var onTime = await world.StartedTripAsync();
        var (_, other) = await world.AnotherStaffAsync();
        var late = await world.StartedTripAsync(other, await world.AnotherVehicleAsync());
        await factory.WithDbAsync(db => db.Trips.Where(t => t.Id == late.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.NextReminderAtUtc, DateTime.UtcNow.AddMinutes(-1))));

        var live = await ApiAssert.OkAsync<List<TripSummaryResponse>>(await world.Admin.GetAsync($"/api/v1/trips/live?lotId={world.Lot.Id}"));

        Assert.Equal([late.Id, onTime.Id], live.Select(t => t.Id));
        Assert.Equal([true, false], live.Select(t => t.IsOverdue));
        await ApiAssert.ErrorAsync(await world.Staff.GetAsync("/api/v1/trips/live"), HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
    }

    [Fact]
    public async Task Staff_hours_add_up_their_segments_in_the_period()
    {
        var world = await TripWorld.CreateAsync(factory);
        var trip = await world.StartedTripAsync();
        await world.Staff.PostAsJsonAsync($"/api/v1/trips/{trip.Id}/end", new { vehicleQrToken = world.Vehicle.QrToken, returnLotId = world.Lot.Id });

        // Make the segment exactly 90 minutes long, on 10 Nov 2026 (GST), for a deterministic check.
        var start = new DateTime(2026, 11, 10, 6, 0, 0, DateTimeKind.Utc);
        await factory.WithDbAsync(async db =>
        {
            await db.TripSegments.Where(s => s.TripId == trip.Id).ExecuteUpdateAsync(s => s
                .SetProperty(x => x.StartedAtUtc, start)
                .SetProperty(x => x.EndedAtUtc, start.AddMinutes(90)));
            await db.Trips.Where(t => t.Id == trip.Id).ExecuteUpdateAsync(s => s
                .SetProperty(x => x.StartedAtUtc, start)
                .SetProperty(x => x.EndedAtUtc, start.AddMinutes(90)));
        });

        var inPeriod = await ApiAssert.OkAsync<StaffTripsResponse>(await world.Admin.GetAsync($"/api/v1/staff/{world.StaffUser.Id}/trips?from=2026-11-10&to=2026-11-10"));
        var otherDay = await ApiAssert.OkAsync<StaffTripsResponse>(await world.Admin.GetAsync($"/api/v1/staff/{world.StaffUser.Id}/trips?from=2026-11-11&to=2026-11-11"));

        Assert.Equal(90 * 60, inPeriod.TotalSeconds);
        Assert.Equal([trip.Id], inPeriod.Trips.Items.Select(t => t.Id));
        Assert.Equal(0, otherDay.TotalSeconds);
        Assert.Empty(otherDay.Trips.Items);
    }
}
