using System.Net;
using System.Net.Http.Json;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Features.Destinations;
using VehicleManagementDM.Api.Features.VehicleCategories;
using VehicleManagementDM.Api.Tests.Infrastructure;

namespace VehicleManagementDM.Api.Tests.MasterData;

/// <summary>Vehicle categories and types (VEH-01) and destinations (DST-01).</summary>
[Collection(ApiCollection.Name)]
public sealed class CatalogueTests(ApiFactory factory)
{
    [Fact]
    public async Task Staff_see_only_active_categories_and_types_admins_can_see_all()
    {
        var admin = await factory.AdminClientAsync();
        var (category, type) = await admin.CreateTypeAsync();
        var hidden = await ApiAssert.OkAsync<TypeResponse>(
            await admin.PostAsJsonAsync($"/api/v1/vehicle-categories/{category.Id}/types", new { name = MasterDataHelpers.Unique("Old type") }));
        await ApiAssert.OkAsync<TypeResponse>(await admin.PutAsJsonAsync($"/api/v1/vehicle-types/{hidden.Id}", new { name = hidden.Name, isActive = false }));

        var staffView = await ApiAssert.OkAsync<List<CategoryResponse>>(await (await factory.StaffClientAsync()).GetAsync("/api/v1/vehicle-categories?includeInactive=true"));
        var adminView = await ApiAssert.OkAsync<List<CategoryResponse>>(await admin.GetAsync("/api/v1/vehicle-categories?includeInactive=true"));

        Assert.Equal([type.Id], staffView.Single(c => c.Id == category.Id).Types.Select(t => t.Id));
        Assert.Equal(2, adminView.Single(c => c.Id == category.Id).Types.Count);
    }

    [Fact]
    public async Task Duplicate_category_name_is_a_field_error()
    {
        var admin = await factory.AdminClientAsync();
        var name = MasterDataHelpers.Unique("Category");
        (await admin.PostAsJsonAsync("/api/v1/vehicle-categories", new { name })).EnsureSuccessStatusCode();

        var response = await admin.PostAsJsonAsync("/api/v1/vehicle-categories", new { name });

        var body = await ApiAssert.ErrorAsync(response, HttpStatusCode.Conflict, ErrorCodes.DuplicateValue);
        Assert.Equal(["A category with this name already exists."], body.Errors!["name"]);
    }

    [Fact]
    public async Task Types_cannot_be_added_to_an_inactive_category()
    {
        var admin = await factory.AdminClientAsync();
        var (category, _) = await admin.CreateTypeAsync();
        (await admin.PutAsJsonAsync($"/api/v1/vehicle-categories/{category.Id}", new { name = category.Name, isActive = false })).EnsureSuccessStatusCode();

        var response = await admin.PostAsJsonAsync($"/api/v1/vehicle-categories/{category.Id}/types", new { name = "Any" });

        await ApiAssert.ErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task Staff_cannot_change_master_data()
    {
        var staff = await factory.StaffClientAsync();

        await ApiAssert.ErrorAsync(await staff.PostAsJsonAsync("/api/v1/vehicle-categories", new { name = "X" }), HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
        await ApiAssert.ErrorAsync(await staff.PostAsJsonAsync("/api/v1/areas", new { name = "X" }), HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
    }

    [Fact]
    public async Task Areas_and_locations_staff_see_active_ones_and_names_are_unique_per_area()
    {
        var admin = await factory.AdminClientAsync();
        var area = await ApiAssert.OkAsync<AreaResponse>(await admin.PostAsJsonAsync("/api/v1/areas", new { name = MasterDataHelpers.Unique("Area") }));
        var market = await ApiAssert.OkAsync<LocationResponse>(await admin.PostAsJsonAsync($"/api/v1/areas/{area.Id}/locations", new { name = "Fish Market" }));
        var closed = await ApiAssert.OkAsync<LocationResponse>(await admin.PostAsJsonAsync($"/api/v1/areas/{area.Id}/locations", new { name = "Old Depot" }));
        (await admin.PutAsJsonAsync($"/api/v1/locations/{closed.Id}", new { name = closed.Name, isActive = false })).EnsureSuccessStatusCode();

        var duplicate = await admin.PostAsJsonAsync($"/api/v1/areas/{area.Id}/locations", new { name = "Fish Market" });
        var staffView = await ApiAssert.OkAsync<List<AreaResponse>>(await (await factory.StaffClientAsync()).GetAsync("/api/v1/areas"));

        var body = await ApiAssert.ErrorAsync(duplicate, HttpStatusCode.Conflict, ErrorCodes.DuplicateValue);
        Assert.Contains("name", body.Errors!.Keys);
        Assert.Equal([market.Id], staffView.Single(a => a.Id == area.Id).Locations.Select(l => l.Id));
    }
}
