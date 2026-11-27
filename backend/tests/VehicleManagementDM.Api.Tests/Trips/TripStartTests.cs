using System.Net;
using System.Net.Http.Json;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Domain.Enums;
using VehicleManagementDM.Api.Features.Auth.Dtos;
using VehicleManagementDM.Api.Features.Lots.Dtos;
using VehicleManagementDM.Api.Features.Trips.Dtos;
using VehicleManagementDM.Api.Tests.Infrastructure;

namespace VehicleManagementDM.Api.Tests.Trips;

/// <summary>Vehicle lookup and trip start (QR-05/06, TRP-01..04, BR-02..04).</summary>
[Collection(ApiCollection.Name)]
public sealed class TripStartTests(ApiFactory factory)
{
    [Fact]
    public async Task Lookup_by_QR_or_typed_registration_shows_the_vehicle_and_that_it_can_start()
    {
        var world = await TripWorld.CreateAsync(factory);
        var typed = world.Vehicle.RegistrationNumber.Replace(" ", "-").ToLowerInvariant();

        var byQr = await ApiAssert.OkAsync<VehicleLookupResponse>(await world.Staff.GetAsync($"/api/v1/vehicles/lookup?qr={world.Vehicle.QrToken}"));
        var byRegistration = await ApiAssert.OkAsync<VehicleLookupResponse>(await world.Staff.GetAsync($"/api/v1/vehicles/lookup?registration={typed}"));

        Assert.True(byQr.CanStart);
        Assert.Null(byQr.BlockReason);
        Assert.Equal(world.Vehicle.Id, byRegistration.Vehicle.Id);
        Assert.Equal(world.Lot.Id, byQr.Vehicle.CurrentLot?.Id);
    }

    [Fact]
    public async Task Lookup_explains_why_a_vehicle_cannot_be_started()
    {
        var world = await TripWorld.CreateAsync(factory);
        var notAtLot = await world.AnotherVehicleAsync(lotId: null);
        var maintenance = await world.AnotherVehicleAsync();
        (await world.Admin.PutAsJsonAsync($"/api/v1/vehicles/{maintenance.Id}/status", new { status = "UnderMaintenance", rowVersion = maintenance.RowVersion })).EnsureSuccessStatusCode();

        var missing = await ApiAssert.OkAsync<VehicleLookupResponse>(await world.Staff.GetAsync($"/api/v1/vehicles/lookup?qr={notAtLot.QrToken}"));
        var inMaintenance = await ApiAssert.OkAsync<VehicleLookupResponse>(await world.Staff.GetAsync($"/api/v1/vehicles/lookup?qr={maintenance.QrToken}"));

        Assert.Equal((false, ErrorCodes.VehicleNotAtLot), (missing.CanStart, missing.BlockReason!.Code));
        Assert.Equal((false, ErrorCodes.VehicleNotAvailable), (inMaintenance.CanStart, inMaintenance.BlockReason!.Code));
        Assert.Contains("maintenance", inMaintenance.BlockReason.Message);
    }

    [Fact]
    public async Task Unknown_QR_is_VEHICLE_NOT_FOUND_and_admins_cannot_scan()
    {
        var world = await TripWorld.CreateAsync(factory);

        await ApiAssert.ErrorAsync(await world.Staff.GetAsync("/api/v1/vehicles/lookup?qr=AAAAAAAAAAAAAAAAAAAAAA"), HttpStatusCode.NotFound, ErrorCodes.VehicleNotFound);
        await ApiAssert.ErrorAsync(await world.Admin.GetAsync($"/api/v1/vehicles/lookup?qr={world.Vehicle.QrToken}"), HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
    }

    [Fact]
    public async Task Start_takes_the_vehicle_out_of_the_lot_and_shows_the_live_trip()
    {
        var world = await TripWorld.CreateAsync(factory, lotCapacity: 5);
        var before = await ApiAssert.OkAsync<LotResponse>(await world.Staff.GetAsync($"/api/v1/lots/{world.Lot.Id}"));

        var response = await world.StartAsync();
        var trip = await ApiAssert.OkAsync<TripDetailResponse>(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(TripStatus.Active, trip.Status);
        Assert.Equal(world.Lot.Id, trip.OriginLot.Id);
        Assert.Equal(world.StaffUser.Id, trip.CurrentStaff.Id);
        Assert.Equal(SegmentKind.Start, Assert.Single(trip.Segments).Kind);
        Assert.False(trip.IsOverdue);
        Assert.NotNull(trip.NextReminderAtUtc);

        var vehicle = await world.AdminVehicleAsync(world.Vehicle.Id);
        Assert.Equal(VehicleStatus.Active, vehicle.Status);
        Assert.Null(vehicle.CurrentLot);
        Assert.False(vehicle.IsNotAtLot);

        var after = await ApiAssert.OkAsync<LotResponse>(await world.Staff.GetAsync($"/api/v1/lots/{world.Lot.Id}"));
        Assert.Equal(before.Total.Occupied - 1, after.Total.Occupied);

        var active = await ApiAssert.OkAsync<TripDetailResponse>(await world.Staff.GetAsync("/api/v1/trips/active"));
        Assert.Equal(trip.Id, active.Id);
        var me = await ApiAssert.OkAsync<MeResponse>(await world.Staff.GetAsync("/api/v1/auth/me"));
        Assert.True(me.HasActiveTrip);
        Assert.Equal(1, me.ThisMonth!.Trips);
    }

    [Fact]
    public async Task Signature_is_stored_and_visible_to_the_driver_and_admins_only()
    {
        var world = await TripWorld.CreateAsync(factory);
        var trip = await world.StartedTripAsync();
        var (_, stranger) = await world.AnotherStaffAsync();

        Assert.NotNull(trip.SignatureUrl);
        Assert.Equal(HttpStatusCode.OK, (await world.Staff.GetAsync(trip.SignatureUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await world.Admin.GetAsync(trip.SignatureUrl)).StatusCode);
        await ApiAssert.ErrorAsync(await stranger.GetAsync(trip.SignatureUrl), HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
    }

    [Fact]
    public async Task One_active_trip_per_staff_member_and_per_vehicle()
    {
        var world = await TripWorld.CreateAsync(factory);
        await world.StartedTripAsync();
        var second = await world.AnotherVehicleAsync();
        var (_, colleague) = await world.AnotherStaffAsync();

        await ApiAssert.ErrorAsync(await world.StartAsync(vehicle: second), HttpStatusCode.Conflict, ErrorCodes.StaffHasActiveTrip);
        await ApiAssert.ErrorAsync(await world.StartAsync(colleague), HttpStatusCode.Conflict, ErrorCodes.VehicleNotAvailable);
    }

    [Fact]
    public async Task Two_phones_starting_the_same_vehicle_at_once_give_exactly_one_trip()
    {
        var world = await TripWorld.CreateAsync(factory);
        var (_, colleague) = await world.AnotherStaffAsync();

        var responses = await Task.WhenAll(world.StartAsync(), world.StartAsync(colleague));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        var loser = Assert.Single(responses, r => r.StatusCode != HttpStatusCode.Created);
        await ApiAssert.ErrorAsync(loser, HttpStatusCode.Conflict, ErrorCodes.VehicleNotAvailable);
    }

    [Fact]
    public async Task Staff_can_start_a_vehicle_from_any_active_lot_but_not_an_inactive_one()
    {
        var world = await TripWorld.CreateAsync(factory);
        var otherLot = await world.Admin.CreateLotAsync();
        var elsewhere = await world.AnotherVehicleAsync(otherLot.Id);
        var closedLot = await world.Admin.CreateLotAsync();
        var inClosedLot = await world.AnotherVehicleAsync(closedLot.Id);
        (await world.Admin.PutAsJsonAsync($"/api/v1/lots/{closedLot.Id}/status", new { isActive = false, rowVersion = closedLot.RowVersion })).EnsureSuccessStatusCode();
        var (_, colleague) = await world.AnotherStaffAsync();

        var trip = await world.StartedTripAsync(vehicle: elsewhere);
        var blocked = await world.StartAsync(colleague, inClosedLot);

        Assert.Equal(otherLot.Id, trip.OriginLot.Id); // origin = where the vehicle was parked (D-047)
        await ApiAssert.ErrorAsync(blocked, HttpStatusCode.Conflict, ErrorCodes.LotInactive);
    }

    [Fact]
    public async Task Bad_signature_and_missing_vehicle_are_rejected()
    {
        var world = await TripWorld.CreateAsync(factory);

        await ApiAssert.ErrorAsync(await world.StartAsync(signature: "not base64!!"), HttpStatusCode.BadRequest, ErrorCodes.InvalidFile);
        await ApiAssert.ErrorAsync(await world.StartAsync(signature: Convert.ToBase64String("GIF89a fake"u8.ToArray())), HttpStatusCode.BadRequest, ErrorCodes.InvalidFile);

        var noVehicle = await world.Staff.PostAsJsonAsync("/api/v1/trips", new { destinationLocationId = world.LocationId, signaturePngBase64 = TripWorld.Signature });
        var body = await ApiAssert.ErrorAsync(noVehicle, HttpStatusCode.BadRequest, ErrorCodes.ValidationFailed);
        Assert.Contains("vehicleQrToken", body.Errors!.Keys);

        // Nothing was started by the failed attempts.
        Assert.Equal(HttpStatusCode.NoContent, (await world.Staff.GetAsync("/api/v1/trips/active")).StatusCode);
    }
}
