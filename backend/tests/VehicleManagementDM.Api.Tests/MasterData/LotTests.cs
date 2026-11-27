using System.Net;
using System.Net.Http.Json;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Features.Lots.Dtos;
using VehicleManagementDM.Api.Tests.Infrastructure;

namespace VehicleManagementDM.Api.Tests.MasterData;

/// <summary>Parking lots, capacities and occupancy (LOT-01..07, BR-05, D-043).</summary>
[Collection(ApiCollection.Name)]
public sealed class LotTests(ApiFactory factory)
{
    [Fact]
    public async Task Occupancy_is_baseline_plus_vehicles_at_the_lot_per_type_and_in_total()
    {
        var admin = await factory.AdminClientAsync();
        var (_, trucks) = await admin.CreateTypeAsync();
        var (_, cars) = await admin.CreateTypeAsync();
        var lot = await admin.CreateLotAsync(totalCapacity: 5, baseline: 2, typeCapacities: [new { vehicleTypeId = trucks.Id, capacity = 1, baselineOccupied = 1 }]);
        await admin.CreateVehicleAsync(trucks.Id, lot.Id);
        await admin.CreateVehicleAsync(cars.Id, lot.Id);

        var result = await ApiAssert.OkAsync<LotResponse>(await admin.GetAsync($"/api/v1/lots/{lot.Id}"));

        Assert.Equal(new OccupancyTotal(5, 4, 1, false, false), result.Total); // 2 baseline + 2 vehicles
        var truckRow = result.ByType.Single(t => t.VehicleTypeId == trucks.Id);
        Assert.Equal((1, 2, -1), (truckRow.Capacity!.Value, truckRow.Occupied, truckRow.Available!.Value)); // over capacity: warning only (BR-09)
        var carRow = result.ByType.Single(t => t.VehicleTypeId == cars.Id);
        Assert.Null(carRow.Capacity);
        Assert.Equal(1, carRow.Occupied);
    }

    [Fact]
    public async Task Lot_codes_are_unique_and_stored_upper_case()
    {
        var admin = await factory.AdminClientAsync();
        var code = "dup" + Guid.NewGuid().ToString("N")[..6];
        var lot = await ApiAssert.OkAsync<LotResponse>(await admin.PostAsJsonAsync("/api/v1/lots", MasterDataHelpers.LotBody(code)));

        var response = await admin.PostAsJsonAsync("/api/v1/lots", MasterDataHelpers.LotBody(code.ToUpperInvariant()));

        Assert.Equal(code.ToUpperInvariant(), lot.Code);
        var body = await ApiAssert.ErrorAsync(response, HttpStatusCode.Conflict, ErrorCodes.DuplicateValue);
        Assert.Contains("code", body.Errors!.Keys);
    }

    [Fact]
    public async Task Inactive_lots_are_hidden_from_staff()
    {
        var admin = await factory.AdminClientAsync();
        var lot = await admin.CreateLotAsync();
        var inactive = await ApiAssert.OkAsync<LotResponse>(await admin.PutAsJsonAsync($"/api/v1/lots/{lot.Id}/status", new { isActive = false, rowVersion = lot.RowVersion }));
        var staff = await factory.StaffClientAsync();

        var staffList = await ApiAssert.OkAsync<List<LotResponse>>(await staff.GetAsync("/api/v1/lots?includeInactive=true"));

        Assert.False(inactive.IsActive);
        Assert.DoesNotContain(staffList, l => l.Id == lot.Id);
        await ApiAssert.ErrorAsync(await staff.GetAsync($"/api/v1/lots/{lot.Id}"), HttpStatusCode.NotFound, ErrorCodes.NotFound);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/v1/lots/{lot.Id}")).StatusCode);
    }

    [Fact]
    public async Task Update_replaces_type_capacities_and_rejects_a_stale_row_version()
    {
        var admin = await factory.AdminClientAsync();
        var (_, first) = await admin.CreateTypeAsync();
        var (_, second) = await admin.CreateTypeAsync();
        var lot = await admin.CreateLotAsync(typeCapacities: [new { vehicleTypeId = first.Id, capacity = 3, baselineOccupied = 0 }]);

        var updated = await ApiAssert.OkAsync<LotResponse>(await admin.PutAsJsonAsync($"/api/v1/lots/{lot.Id}", WithRowVersion(
            MasterDataHelpers.LotBody(lot.Code, typeCapacities: [new { vehicleTypeId = second.Id, capacity = 7, baselineOccupied = 1 }]),
            lot.RowVersion)));
        var stale = await admin.PutAsJsonAsync($"/api/v1/lots/{lot.Id}", WithRowVersion(MasterDataHelpers.LotBody(lot.Code), lot.RowVersion));

        Assert.Equal([(second.Id, (int?)7)], updated.ByType.Select(t => (t.VehicleTypeId, t.Capacity)));
        await ApiAssert.ErrorAsync(stale, HttpStatusCode.Conflict, ErrorCodes.ConcurrencyConflict);
    }

    [Fact]
    public async Task Invalid_coordinates_are_field_errors()
    {
        var admin = await factory.AdminClientAsync();
        var body = MasterDataHelpers.LotBody();
        var json = System.Text.Json.JsonSerializer.SerializeToNode(body)!;
        json["latitude"] = 123;

        var response = await admin.PostAsJsonAsync("/api/v1/lots", json);

        var error = await ApiAssert.ErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.ValidationFailed);
        Assert.Equal(["Latitude must be between -90 and 90."], error.Errors!["latitude"]);
    }

    private static System.Text.Json.Nodes.JsonNode WithRowVersion(object body, byte[] rowVersion)
    {
        var json = System.Text.Json.JsonSerializer.SerializeToNode(body)!;
        json["rowVersion"] = Convert.ToBase64String(rowVersion);
        return json;
    }
}
