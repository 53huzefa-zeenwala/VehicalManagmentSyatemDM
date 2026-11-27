using System.Net.Http.Json;
using VehicleManagementDM.Api.Features.Accounts;
using VehicleManagementDM.Api.Features.Destinations;
using VehicleManagementDM.Api.Features.Lots.Dtos;
using VehicleManagementDM.Api.Features.Trips.Dtos;
using VehicleManagementDM.Api.Features.VehicleCategories;
using VehicleManagementDM.Api.Features.Vehicles.Dtos;
using VehicleManagementDM.Api.Tests.Files;

namespace VehicleManagementDM.Api.Tests.Infrastructure;

/// <summary>A small, isolated world for trip tests: a lot, a vehicle type, vehicles, a destination and a signed-in staff member.</summary>
public sealed record TripWorld(
    ApiFactory Factory,
    HttpClient Admin,
    HttpClient Staff,
    StaffResponse StaffUser,
    LotResponse Lot,
    TypeResponse Type,
    VehicleResponse Vehicle,
    int LocationId)
{
    public static readonly string Signature = Convert.ToBase64String(FileStoreTests.Png);

    public static async Task<TripWorld> CreateAsync(ApiFactory factory, int lotCapacity = 10)
    {
        var admin = await factory.AdminClientAsync();
        var (_, type) = await admin.CreateTypeAsync();
        var lot = await admin.CreateLotAsync(totalCapacity: lotCapacity);
        var vehicle = await admin.CreateVehicleAsync(type.Id, lot.Id);
        var area = await ApiAssert.OkAsync<AreaResponse>(await admin.PostAsJsonAsync("/api/v1/areas", new { name = MasterDataHelpers.Unique("Area") }));
        var location = await ApiAssert.OkAsync<LocationResponse>(await admin.PostAsJsonAsync($"/api/v1/areas/{area.Id}/locations", new { name = "Fish Market" }));
        var (staffUser, staff) = await NewStaffAsync(factory, admin);
        return new TripWorld(factory, admin, staff, staffUser, lot, type, vehicle, location.Id);
    }

    public static async Task<(StaffResponse User, HttpClient Client)> NewStaffAsync(ApiFactory factory, HttpClient admin)
    {
        var user = await admin.CreateStaffAsync();
        var client = factory.CreateApiClient();
        (await client.LoginAsync(user.LoginId, ApiFactory.DefaultPassword)).EnsureSuccessStatusCode();
        return (user, client);
    }

    public Task<(StaffResponse User, HttpClient Client)> AnotherStaffAsync() => NewStaffAsync(Factory, Admin);

    public Task<VehicleResponse> AnotherVehicleAsync(int? lotId = -1) => Admin.CreateVehicleAsync(Type.Id, lotId == -1 ? Lot.Id : lotId);

    public Task<HttpResponseMessage> StartAsync(HttpClient? staff = null, VehicleResponse? vehicle = null, string? signature = null) =>
        (staff ?? Staff).PostAsJsonAsync("/api/v1/trips", new
        {
            vehicleQrToken = (vehicle ?? Vehicle).QrToken,
            destinationLocationId = LocationId,
            signaturePngBase64 = signature ?? Signature,
        });

    public async Task<TripDetailResponse> StartedTripAsync(HttpClient? staff = null, VehicleResponse? vehicle = null) =>
        await ApiAssert.OkAsync<TripDetailResponse>(await StartAsync(staff, vehicle));

    public async Task<VehicleResponse> AdminVehicleAsync(int id) =>
        await ApiAssert.OkAsync<VehicleResponse>(await Admin.GetAsync($"/api/v1/vehicles/{id}"));
}
