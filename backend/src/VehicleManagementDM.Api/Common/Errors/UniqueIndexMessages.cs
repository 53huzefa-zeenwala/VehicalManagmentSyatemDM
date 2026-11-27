using VehicleManagementDM.Api.Data.Configurations;

namespace VehicleManagementDM.Api.Common.Errors;

/// <summary>
/// Turns a unique-index name from a SQL Server duplicate-key error into the error code, the JSON field
/// and the user message (ARCHITECTURE.md §5.2). Add an entry whenever a unique index is added.
/// </summary>
internal static class UniqueIndexMessages
{
    public sealed record Entry(string Code, string Title, string? Field, string Message);

    private static readonly Dictionary<string, Entry> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        [UserConfiguration.LoginIdIndex] = Duplicate("loginId", "This login ID already exists."),
        [VehicleCategoryConfiguration.NameIndex] = Duplicate("name", "A category with this name already exists."),
        [VehicleTypeConfiguration.NameIndex] = Duplicate("name", "This category already has a type with this name."),
        [VehicleConfiguration.RegistrationIndex] = Duplicate("registrationNumber", "A vehicle with this registration number already exists."),
        [VehicleConfiguration.QrTokenIndex] = Duplicate(null, "Could not create a unique QR code. Try again."),
        [ParkingLotConfiguration.CodeIndex] = Duplicate("code", "A parking lot with this code already exists."),
        [AreaConfiguration.NameIndex] = Duplicate("name", "An area with this name already exists."),
        [LocationConfiguration.NameIndex] = Duplicate("name", "This area already has a location with this name."),

        // Last-line guards for races between two phones (BR-02); services normally catch these first.
        [TripConfiguration.ActiveStaffIndex] = new(ErrorCodes.StaffHasActiveTrip, "Trip already running", null, "This staff member already has an active trip."),
        [TripConfiguration.ActiveVehicleIndex] = new(ErrorCodes.VehicleNotAvailable, "Vehicle not available", null, "This vehicle was just taken on another trip."),
        [TransferConfiguration.OpenVehicleIndex] = new(ErrorCodes.VehicleInTransfer, "Vehicle in transfer", null, "This vehicle already has an open transfer."),
    };

    public static Entry For(string? indexName) =>
        indexName is not null && Map.TryGetValue(indexName, out var entry)
            ? entry
            : Duplicate(null, "A record with the same value already exists.");

    private static Entry Duplicate(string? field, string message) => new(ErrorCodes.DuplicateValue, "Already exists", field, message);
}
