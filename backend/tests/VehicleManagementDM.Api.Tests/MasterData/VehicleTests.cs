using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Paging;
using VehicleManagementDM.Api.Domain.Enums;
using VehicleManagementDM.Api.Features.Vehicles.Dtos;
using VehicleManagementDM.Api.Tests.Files;
using VehicleManagementDM.Api.Tests.Infrastructure;

namespace VehicleManagementDM.Api.Tests.MasterData;

/// <summary>Vehicle catalogue, statuses, locations, photos and QR labels (VEH, QR, D-034, D-040, D-043).</summary>
[Collection(ApiCollection.Name)]
public sealed class VehicleTests(ApiFactory factory)
{
    [Fact]
    public async Task Created_vehicle_gets_a_random_QR_token_and_an_audit_entry()
    {
        var admin = await factory.AdminClientAsync();
        var (_, type) = await admin.CreateTypeAsync();
        var lot = await admin.CreateLotAsync();

        var vehicle = await admin.CreateVehicleAsync(type.Id, lot.Id, "d-0 1423x");

        Assert.Equal("D-0 1423X", vehicle.RegistrationNumber);
        Assert.Matches("^[A-Za-z0-9_-]{22}$", vehicle.QrToken);
        Assert.Equal(VehicleStatus.Available, vehicle.Status);
        Assert.Equal(lot.Id, vehicle.CurrentLot?.Id);
        Assert.False(vehicle.IsNotAtLot);

        var audit = await factory.WithDbAsync((db, _) =>
            db.AuditLogs.AsNoTracking().SingleAsync(a => a.EntityType == "Vehicle" && a.EntityId == vehicle.Id));
        Assert.Equal("Create", audit.Action);
        Assert.Contains("\"registrationNumber\":\"D-0 1423X\"", audit.AfterJson);
    }

    [Fact]
    public async Task Registration_numbers_are_unique_ignoring_case_spaces_and_dashes()
    {
        var admin = await factory.AdminClientAsync();
        var (_, type) = await admin.CreateTypeAsync();
        var registration = "Q " + Random.Shared.Next(100_000, 999_999);
        await admin.CreateVehicleAsync(type.Id, null, registration);

        var response = await admin.PostAsJsonAsync("/api/v1/vehicles", new
        {
            registrationNumber = registration.Replace(" ", "-").ToLowerInvariant(),
            vehicleTypeId = type.Id,
            brand = "B",
            model = "M",
            colour = "C",
        });

        var body = await ApiAssert.ErrorAsync(response, HttpStatusCode.Conflict, ErrorCodes.DuplicateValue);
        Assert.Contains("registrationNumber", body.Errors!.Keys);
    }

    [Fact]
    public async Task Unknown_vehicle_type_is_a_field_error()
    {
        var admin = await factory.AdminClientAsync();

        var response = await admin.PostAsJsonAsync("/api/v1/vehicles", new
        {
            registrationNumber = MasterDataHelpers.UniqueRegistration(),
            vehicleTypeId = int.MaxValue,
            brand = "B",
            model = "M",
            colour = "C",
        });

        var body = await ApiAssert.ErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.ValidationFailed);
        Assert.Contains("vehicleTypeId", body.Errors!.Keys);
    }

    [Fact]
    public async Task Status_changes_check_the_row_version_and_never_allow_Active()
    {
        var admin = await factory.AdminClientAsync();
        var (_, type) = await admin.CreateTypeAsync();
        var vehicle = await admin.CreateVehicleAsync(type.Id, null);

        var maintenance = await ApiAssert.OkAsync<VehicleResponse>(await admin.PutAsJsonAsync(
            $"/api/v1/vehicles/{vehicle.Id}/status", new { status = "UnderMaintenance", rowVersion = vehicle.RowVersion }));
        var stale = await admin.PutAsJsonAsync($"/api/v1/vehicles/{vehicle.Id}/status", new { status = "Available", rowVersion = vehicle.RowVersion });
        var active = await admin.PutAsJsonAsync($"/api/v1/vehicles/{vehicle.Id}/status", new { status = "Active", rowVersion = maintenance.RowVersion });

        Assert.Equal(VehicleStatus.UnderMaintenance, maintenance.Status);
        await ApiAssert.ErrorAsync(stale, HttpStatusCode.Conflict, ErrorCodes.ConcurrencyConflict);
        await ApiAssert.ErrorAsync(active, HttpStatusCode.BadRequest, ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task Status_cannot_change_while_the_vehicle_is_out()
    {
        var admin = await factory.AdminClientAsync();
        var (_, type) = await admin.CreateTypeAsync();
        var vehicle = await admin.CreateVehicleAsync(type.Id, null);
        await factory.WithDbAsync(db => db.Vehicles.Where(v => v.Id == vehicle.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.Status, VehicleStatus.Active)));
        var current = await ApiAssert.OkAsync<VehicleResponse>(await admin.GetAsync($"/api/v1/vehicles/{vehicle.Id}"));

        var response = await admin.PutAsJsonAsync($"/api/v1/vehicles/{vehicle.Id}/status", new { status = "Inactive", rowVersion = current.RowVersion });

        await ApiAssert.ErrorAsync(response, HttpStatusCode.Conflict, ErrorCodes.StatusChangeNotAllowed);
    }

    [Fact]
    public async Task Not_at_a_lot_vehicles_are_listed_until_an_admin_places_them()
    {
        var admin = await factory.AdminClientAsync();
        var (_, type) = await admin.CreateTypeAsync();
        var lot = await admin.CreateLotAsync();
        var vehicle = await admin.CreateVehicleAsync(type.Id, null);

        var before = await ApiAssert.OkAsync<PagedResult<VehicleResponse>>(await admin.GetAsync($"/api/v1/vehicles?notAtLot=true&typeId={type.Id}"));
        var placed = await ApiAssert.OkAsync<VehicleResponse>(await admin.PutAsJsonAsync(
            $"/api/v1/vehicles/{vehicle.Id}/location", new { lotId = lot.Id, rowVersion = vehicle.RowVersion }));
        var after = await ApiAssert.OkAsync<PagedResult<VehicleResponse>>(await admin.GetAsync($"/api/v1/vehicles?notAtLot=true&typeId={type.Id}"));

        Assert.Equal([vehicle.Id], before.Items.Select(v => v.Id));
        Assert.True(vehicle.IsNotAtLot);
        Assert.Equal(lot.Id, placed.CurrentLot?.Id);
        Assert.Empty(after.Items);
        var actions = await factory.WithDbAsync((db, _) =>
            db.AuditLogs.AsNoTracking().Where(a => a.EntityType == "Vehicle" && a.EntityId == vehicle.Id).Select(a => a.Action).ToListAsync());
        Assert.Contains("LocationResolve", actions);
    }

    [Fact]
    public async Task Photo_upload_stores_a_png_and_replacing_it_removes_the_old_file()
    {
        var admin = await factory.AdminClientAsync();
        var (_, type) = await admin.CreateTypeAsync();
        var vehicle = await admin.CreateVehicleAsync(type.Id, null);

        var first = await ApiAssert.OkAsync<VehicleResponse>(await admin.PutAsync($"/api/v1/vehicles/{vehicle.Id}/photo", PhotoContent(FileStoreTests.Png, "a.png")));
        var second = await ApiAssert.OkAsync<VehicleResponse>(await admin.PutAsync($"/api/v1/vehicles/{vehicle.Id}/photo", PhotoContent(FileStoreTests.Jpeg, "b.jpg")));

        Assert.NotNull(first.PhotoUrl);
        Assert.EndsWith(".jpg", second.PhotoUrl);
        Assert.Equal(FileStoreTests.Jpeg, await (await admin.GetAsync(second.PhotoUrl)).Content.ReadAsByteArrayAsync());
        Assert.False(File.Exists(Path.Combine(factory.FilesRoot, "vehicles", first.PhotoUrl!.Split('/')[^1])));
    }

    [Fact]
    public async Task Photo_that_is_not_an_image_is_INVALID_FILE()
    {
        var admin = await factory.AdminClientAsync();
        var (_, type) = await admin.CreateTypeAsync();
        var vehicle = await admin.CreateVehicleAsync(type.Id, null);

        var response = await admin.PutAsync($"/api/v1/vehicles/{vehicle.Id}/photo", PhotoContent("MZ fake exe"u8.ToArray(), "photo.png"));

        await ApiAssert.ErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.InvalidFile);
    }

    [Fact]
    public async Task QR_labels_return_label_data_for_the_app()
    {
        var admin = await factory.AdminClientAsync();
        var (category, type) = await admin.CreateTypeAsync();
        var one = await admin.CreateVehicleAsync(type.Id, null);
        var two = await admin.CreateVehicleAsync(type.Id, null);

        var labels = await ApiAssert.OkAsync<List<QrLabelResponse>>(await admin.GetAsync($"/api/v1/vehicles/qr-labels?ids={one.Id}&ids={two.Id}"));
        var missing = await admin.GetAsync($"/api/v1/vehicles/qr-labels?ids={one.Id}&ids={int.MaxValue}");

        Assert.Equal(2, labels.Count);
        Assert.All(labels, l => Assert.Equal(category.Name, l.Category));
        Assert.Contains(labels, l => l.QrToken == one.QrToken && l.RegistrationNumber == one.RegistrationNumber);
        await ApiAssert.ErrorAsync(missing, HttpStatusCode.NotFound, ErrorCodes.NotFound);
    }

    [Fact]
    public async Task Staff_cannot_use_vehicle_management()
    {
        var staff = await factory.StaffClientAsync();

        await ApiAssert.ErrorAsync(await staff.GetAsync("/api/v1/vehicles"), HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
    }

    private static MultipartFormDataContent PhotoContent(byte[] bytes, string fileName)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        return new MultipartFormDataContent { { file, "file", fileName } };
    }
}
