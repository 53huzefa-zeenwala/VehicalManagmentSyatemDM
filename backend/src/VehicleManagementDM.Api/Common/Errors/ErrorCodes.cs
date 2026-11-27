namespace VehicleManagementDM.Api.Common.Errors;

/// <summary>
/// Machine-readable error codes (ARCHITECTURE.md §5.2, API_DOCUMENTATION.md §2).
/// Never rename a published code: the app depends on them.
/// </summary>
public static class ErrorCodes
{
    // Request problems
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string MalformedRequest = "MALFORMED_REQUEST";
    public const string InvalidFile = "INVALID_FILE";
    public const string PayloadTooLarge = "PAYLOAD_TOO_LARGE";
    public const string UnsupportedMediaType = "UNSUPPORTED_MEDIA_TYPE";
    public const string MethodNotAllowed = "METHOD_NOT_ALLOWED";
    public const string RouteNotFound = "ROUTE_NOT_FOUND";
    public const string TooManyRequests = "TOO_MANY_REQUESTS";
    public const string RequestFailed = "REQUEST_FAILED";

    // Authentication and authorization
    public const string Unauthenticated = "UNAUTHENTICATED";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string AccountLocked = "ACCOUNT_LOCKED";
    public const string AccountDeactivated = "ACCOUNT_DEACTIVATED";
    public const string Forbidden = "FORBIDDEN";

    // Data
    public const string NotFound = "NOT_FOUND";
    public const string FileNotFound = "FILE_NOT_FOUND";
    public const string DuplicateValue = "DUPLICATE_VALUE";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    public const string IdSequenceExhausted = "ID_SEQUENCE_EXHAUSTED";

    // Business rules (API_DOCUMENTATION.md §2)
    public const string StatusChangeNotAllowed = "STATUS_CHANGE_NOT_ALLOWED";
    public const string VehicleNotAvailable = "VEHICLE_NOT_AVAILABLE";
    public const string CannotDeactivateSelf = "CANNOT_DEACTIVATE_SELF";
    public const string VehicleNotFound = "VEHICLE_NOT_FOUND";
    public const string VehicleNotAtLot = "VEHICLE_NOT_AT_LOT";
    public const string VehicleMismatch = "VEHICLE_MISMATCH";
    public const string StaffHasActiveTrip = "STAFF_HAS_ACTIVE_TRIP";
    public const string LotInactive = "LOT_INACTIVE";
    public const string TripNotActive = "TRIP_NOT_ACTIVE";
    public const string NotYourTrip = "NOT_YOUR_TRIP";
    public const string HandoverTargetInvalid = "HANDOVER_TARGET_INVALID";
    public const string InvalidIdPrefix = "INVALID_ID_PREFIX";
    public const string VehicleInTransfer = "VEHICLE_IN_TRANSFER";
    public const string TransferInvalidState = "TRANSFER_INVALID_STATE";

    // Warnings returned inside successful responses (never block, BR-09)
    public const string LotFull = "LOT_FULL";

    // Server and database
    public const string InternalError = "INTERNAL_ERROR";
    public const string DatabaseSchemaError = "DATABASE_SCHEMA_ERROR";
    public const string DatabaseUnavailable = "DATABASE_UNAVAILABLE";
    public const string DatabaseNotReady = "DATABASE_NOT_READY";
    public const string DatabaseTimeout = "DATABASE_TIMEOUT";

    /// <summary>Codes meaning the database can't be used right now; their logs go to the fallback file.</summary>
    public static bool IsDatabaseOutage(string code) =>
        code is DatabaseUnavailable or DatabaseNotReady or DatabaseTimeout;
}
