using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Domain.Enums;
using VehicleManagementDM.Api.Features.Lots.Dtos;
using VehicleManagementDM.Api.Features.Notifications;
using VehicleManagementDM.Api.Features.Transfers;
using VehicleManagementDM.Api.Features.Trips.Dtos;
using VehicleManagementDM.Api.Tests.Infrastructure;

namespace VehicleManagementDM.Api.Tests.Transfers;

/// <summary>Transfers between lots (TRF-01..07, D-020, D-026, D-047).</summary>
[Collection(ApiCollection.Name)]
public sealed class TransferTests(ApiFactory factory)
{
    [Fact]
    public async Task Full_transfer_counts_at_the_origin_until_arrival_and_the_confirmer_drives()
    {
        var world = await TripWorld.CreateAsync(factory);
        var destination = await world.Admin.CreateLotAsync();

        var created = await CreateAsync(world, destination.Id);
        Assert.Equal(TransferStatus.PendingDeparture, created.Status);
        Assert.Contains(
            (await ApiAssert.OkAsync<NotificationPage>(await world.Staff.GetAsync("/api/v1/notifications"))).Items,
            n => n.Type == NotificationType.TransferAwaitingDeparture && n.TargetId == created.Id);

        var tasks = await ApiAssert.OkAsync<TransferTasksResponse>(await world.Staff.GetAsync("/api/v1/transfers/tasks"));
        Assert.Contains(tasks.AwaitingDeparture, t => t.Id == created.Id);

        var departed = await ApiAssert.OkAsync<TransferResponse>(await world.Staff.PostAsync($"/api/v1/transfers/{created.Id}/depart", null));
        Assert.Equal((TransferStatus.InTransit, world.StaffUser.Id), (departed.Status, departed.Driver!.Id));
        var vehicle = await world.AdminVehicleAsync(world.Vehicle.Id);
        Assert.Equal((VehicleStatus.Active, world.Lot.Id), (vehicle.Status, vehicle.CurrentLot!.Id)); // still counted at origin (TRF-03)

        var (_, receiver) = await world.AnotherStaffAsync();
        var byScan = await ApiAssert.OkAsync<TransferResponse>(await receiver.GetAsync($"/api/v1/transfers/by-vehicle?qr={world.Vehicle.QrToken}"));
        var arrived = await ApiAssert.OkAsync<TransferResponse>(await receiver.PostAsync($"/api/v1/transfers/{byScan.Id}/arrive", null));

        Assert.Equal(TransferStatus.Completed, arrived.Status);
        Assert.NotNull(arrived.DrivingSeconds);
        vehicle = await world.AdminVehicleAsync(world.Vehicle.Id);
        Assert.Equal((VehicleStatus.Available, destination.Id), (vehicle.Status, vehicle.CurrentLot!.Id));

        var history = await ApiAssert.OkAsync<VehicleManagementDM.Api.Common.Paging.PagedResult<VehicleManagementDM.Api.Features.Vehicles.VehicleHistoryEntry>>(
            await world.Admin.GetAsync($"/api/v1/vehicles/{world.Vehicle.Id}/history"));
        Assert.Contains(history.Items, e => e.Kind == "Transfer" && e.TransferId == created.Id);
        Assert.Contains(history.Items, e => e.Kind == "Create");
    }

    [Fact]
    public async Task A_vehicle_waiting_for_transfer_cannot_start_a_trip()
    {
        var world = await TripWorld.CreateAsync(factory);
        var destination = await world.Admin.CreateLotAsync();
        await CreateAsync(world, destination.Id);

        var lookup = await ApiAssert.OkAsync<VehicleLookupResponse>(await world.Staff.GetAsync($"/api/v1/vehicles/lookup?qr={world.Vehicle.QrToken}"));
        var start = await world.StartAsync();
        var second = await world.Admin.PostAsJsonAsync("/api/v1/transfers", new { vehicleId = world.Vehicle.Id, toLotId = destination.Id });

        Assert.Equal(ErrorCodes.VehicleInTransfer, lookup.BlockReason!.Code);
        await ApiAssert.ErrorAsync(start, HttpStatusCode.Conflict, ErrorCodes.VehicleInTransfer);
        await ApiAssert.ErrorAsync(second, HttpStatusCode.Conflict, ErrorCodes.VehicleInTransfer);
    }

    [Fact]
    public async Task Driving_a_transfer_counts_as_the_one_active_trip()
    {
        var world = await TripWorld.CreateAsync(factory);
        var destination = await world.Admin.CreateLotAsync();
        var transfer = await CreateAsync(world, destination.Id);
        var other = await world.AnotherVehicleAsync();
        (await world.Staff.PostAsync($"/api/v1/transfers/{transfer.Id}/depart", null)).EnsureSuccessStatusCode();

        var (_, onTrip) = await world.AnotherStaffAsync();
        await world.StartedTripAsync(onTrip, other);
        var secondTransfer = await ApiAssert.OkAsync<TransferResponse>(await world.Admin.PostAsJsonAsync(
            "/api/v1/transfers", new { vehicleId = (await world.AnotherVehicleAsync()).Id, toLotId = destination.Id }));

        await ApiAssert.ErrorAsync(await world.StartAsync(vehicle: await world.AnotherVehicleAsync()), HttpStatusCode.Conflict, ErrorCodes.StaffHasActiveTrip);
        await ApiAssert.ErrorAsync(await onTrip.PostAsync($"/api/v1/transfers/{secondTransfer.Id}/depart", null), HttpStatusCode.Conflict, ErrorCodes.StaffHasActiveTrip);
        var candidates = await ApiAssert.OkAsync<List<StaffRef>>(await onTrip.GetAsync("/api/v1/staff/handover-candidates"));
        Assert.DoesNotContain(candidates, c => c.Id == world.StaffUser.Id);
    }

    [Fact]
    public async Task Cancel_only_before_departure_and_invalid_targets_are_refused()
    {
        var world = await TripWorld.CreateAsync(factory);
        var destination = await world.Admin.CreateLotAsync();
        var closed = await world.Admin.CreateLotAsync();
        (await world.Admin.PutAsJsonAsync($"/api/v1/lots/{closed.Id}/status", new { isActive = false, rowVersion = closed.RowVersion })).EnsureSuccessStatusCode();

        var sameLot = await world.Admin.PostAsJsonAsync("/api/v1/transfers", new { vehicleId = world.Vehicle.Id, toLotId = world.Lot.Id });
        var inactive = await world.Admin.PostAsJsonAsync("/api/v1/transfers", new { vehicleId = world.Vehicle.Id, toLotId = closed.Id });
        var first = await CreateAsync(world, destination.Id);
        var cancelled = await ApiAssert.OkAsync<TransferResponse>(await world.Admin.PostAsync($"/api/v1/transfers/{first.Id}/cancel", null));
        var second = await CreateAsync(world, destination.Id);
        (await world.Staff.PostAsync($"/api/v1/transfers/{second.Id}/depart", null)).EnsureSuccessStatusCode();
        var tooLate = await world.Admin.PostAsync($"/api/v1/transfers/{second.Id}/cancel", null);
        var departAgain = await world.Staff.PostAsync($"/api/v1/transfers/{second.Id}/depart", null);

        await ApiAssert.ErrorAsync(sameLot, HttpStatusCode.BadRequest, ErrorCodes.ValidationFailed);
        await ApiAssert.ErrorAsync(inactive, HttpStatusCode.Conflict, ErrorCodes.LotInactive);
        Assert.Equal(TransferStatus.Cancelled, cancelled.Status);
        await ApiAssert.ErrorAsync(tooLate, HttpStatusCode.Conflict, ErrorCodes.TransferInvalidState);
        await ApiAssert.ErrorAsync(departAgain, HttpStatusCode.Conflict, ErrorCodes.TransferInvalidState);
        await ApiAssert.ErrorAsync(await world.Staff.PostAsJsonAsync("/api/v1/transfers", new { vehicleId = 1, toLotId = 1 }), HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
    }

    [Fact]
    public async Task Transfer_driving_time_counts_in_staff_hours()
    {
        var world = await TripWorld.CreateAsync(factory);
        var destination = await world.Admin.CreateLotAsync();
        var transfer = await CreateAsync(world, destination.Id);
        (await world.Staff.PostAsync($"/api/v1/transfers/{transfer.Id}/depart", null)).EnsureSuccessStatusCode();
        (await world.Staff.PostAsync($"/api/v1/transfers/{transfer.Id}/arrive", null)).EnsureSuccessStatusCode();
        var departed = new DateTime(2026, 11, 12, 6, 0, 0, DateTimeKind.Utc);
        await factory.WithDbAsync(db => db.Transfers.Where(t => t.Id == transfer.Id).ExecuteUpdateAsync(s => s
            .SetProperty(t => t.DepartedAtUtc, departed)
            .SetProperty(t => t.ArrivedAtUtc, departed.AddMinutes(45))));

        var hours = await ApiAssert.OkAsync<StaffTripsResponse>(await world.Admin.GetAsync($"/api/v1/staff/{world.StaffUser.Id}/trips?from=2026-11-12&to=2026-11-12"));

        Assert.Equal(45 * 60, hours.TotalSeconds);
    }

    private static async Task<TransferResponse> CreateAsync(TripWorld world, int toLotId)
    {
        var response = await world.Admin.PostAsJsonAsync("/api/v1/transfers", new { vehicleId = world.Vehicle.Id, toLotId, note = "Rebalance" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ApiAssert.OkAsync<TransferResponse>(response);
    }
}
