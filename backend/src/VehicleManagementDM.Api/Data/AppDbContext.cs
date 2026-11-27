using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VehicleManagementDM.Api.Domain.Entities;

namespace VehicleManagementDM.Api.Data;

/// <summary>The app's only database context. Schema changes go through migrations (D-030).</summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<IdSequence> IdSequences => Set<IdSequence>();

    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<VehicleCategory> VehicleCategories => Set<VehicleCategory>();

    public DbSet<VehicleType> VehicleTypes => Set<VehicleType>();

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<ParkingLot> ParkingLots => Set<ParkingLot>();

    public DbSet<LotTypeCapacity> LotTypeCapacities => Set<LotTypeCapacity>();

    public DbSet<Area> Areas => Set<Area>();

    public DbSet<Location> Locations => Set<Location>();

    public DbSet<Trip> Trips => Set<Trip>();

    public DbSet<TripSegment> TripSegments => Set<TripSegment>();

    public DbSet<Transfer> Transfers => Set<Transfer>();

    public DbSet<ReminderLog> ReminderLogs => Set<ReminderLog>();

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // All times are UTC (BR-01): stored as datetime2(3) and read back with Kind = Utc,
        // so JSON always carries the "Z" suffix.
        configurationBuilder.Properties<DateTime>()
            .HavePrecision(3)
            .HaveConversion<UtcDateTimeConverter>();

        // Enums are stored as readable strings (DATABASE_DESIGN.md §1).
        configurationBuilder.Properties<Enum>()
            .HaveConversion<string>()
            .HaveMaxLength(30)
            .AreUnicode(false);
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        value => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(),
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
