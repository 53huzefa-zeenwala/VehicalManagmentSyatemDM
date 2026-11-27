namespace VehicleManagementDM.Api.Common.Storage;

/// <summary>Configuration section "Storage" (D-034, D-042, ARCHITECTURE.md §11).</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Root folder for photos and signatures. Relative paths are resolved from the content root.
    /// In production it must be outside the publish folder so deployments never touch it.
    /// </summary>
    public string RootPath { get; set; } = "App_Data/files";

    /// <summary>Largest vehicle or staff photo accepted (20 MB, D-042).</summary>
    public long MaxPhotoBytes { get; set; } = 20 * 1024 * 1024;

    /// <summary>Largest signature image accepted (512 KB).</summary>
    public long MaxSignatureBytes { get; set; } = 512 * 1024;
}
