using System.Net.Http.Json;
using VehicleManagementDM.Api.Features.Accounts;
using VehicleManagementDM.Api.Features.Lots.Dtos;
using VehicleManagementDM.Api.Features.VehicleCategories;
using VehicleManagementDM.Api.Features.Vehicles.Dtos;

namespace VehicleManagementDM.Api.Tests.Infrastructure;

/// <summary>Creates master data through the real API. Names are unique per call because tests share one database.</summary>
public static class MasterDataHelpers
{
    public static string Unique(string prefix) => $"{prefix} {Guid.NewGuid().ToString("N")[..8]}";

    public static async Task<(CategoryResponse Category, TypeResponse Type)> CreateTypeAsync(this HttpClient admin)
    {
        var category = await ApiAssert.OkAsync<CategoryResponse>(
            await admin.PostAsJsonAsync("/api/v1/vehicle-categories", new { name = Unique("Category") }));
        var type = await ApiAssert.OkAsync<TypeResponse>(
            await admin.PostAsJsonAsync($"/api/v1/vehicle-categories/{category.Id}/types", new { name = Unique("Type") }));
        return (category, type);
    }

    public static object LotBody(string? code = null, int totalCapacity = 10, int baseline = 0, object[]? typeCapacities = null) => new
    {
        code = code ?? "L" + Guid.NewGuid().ToString("N")[..8],
        name = Unique("Lot"),
        address = "Al Qusais Industrial Area 2, Dubai",
        contactName = "Lot Supervisor",
        contactNumber = "+971500000010",
        opensAt = "06:00",
        closesAt = "22:00",
        lotType = "Open",
        totalCapacity,
        baselineOccupied = baseline,
        typeCapacities = typeCapacities ?? [],
    };

    public static async Task<LotResponse> CreateLotAsync(this HttpClient admin, int totalCapacity = 10, int baseline = 0, object[]? typeCapacities = null) =>
        await ApiAssert.OkAsync<LotResponse>(
            await admin.PostAsJsonAsync("/api/v1/lots", LotBody(totalCapacity: totalCapacity, baseline: baseline, typeCapacities: typeCapacities)));

    public static string UniqueRegistration() => "T " + Random.Shared.Next(100_000, 999_999);

    public static async Task<VehicleResponse> CreateVehicleAsync(this HttpClient admin, int typeId, int? lotId, string? registration = null) =>
        await ApiAssert.OkAsync<VehicleResponse>(await admin.PostAsJsonAsync("/api/v1/vehicles", new
        {
            registrationNumber = registration ?? UniqueRegistration(),
            vehicleTypeId = typeId,
            brand = "Isuzu",
            model = "NPR",
            colour = "White",
            lotId,
        }));

    public static async Task<StaffResponse> CreateStaffAsync(this HttpClient admin) =>
        await ApiAssert.OkAsync<StaffResponse>(await admin.PostAsJsonAsync("/api/v1/staff", new
        {
            fullName = Unique("Staff"),
            mobile = "+971500000020",
            email = (string?)null,
            licenceNumber = "LIC-" + Guid.NewGuid().ToString("N")[..6],
            password = ApiFactory.DefaultPassword,
        }));
}
