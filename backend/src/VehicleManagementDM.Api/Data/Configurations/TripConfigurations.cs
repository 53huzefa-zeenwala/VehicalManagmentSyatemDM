using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleManagementDM.Api.Domain.Entities;

namespace VehicleManagementDM.Api.Data.Configurations;

internal sealed class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    /// <summary>One active trip per staff member (BR-02): the database's last line of defence.</summary>
    public const string ActiveStaffIndex = "UX_Trips_ActiveStaff";

    /// <summary>One active trip per vehicle (BR-02).</summary>
    public const string ActiveVehicleIndex = "UX_Trips_ActiveVehicle";

    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable("Trips", table =>
            table.HasCheckConstraint("CK_Trips_EndedAfterStart", "[EndedAtUtc] IS NULL OR [EndedAtUtc] >= [StartedAtUtc]"));
        builder.Property(t => t.CancelNote).HasMaxLength(500);
        builder.Property(t => t.RowVersion).IsRowVersion();

        builder.HasIndex(t => t.CurrentStaffId).IsUnique().HasFilter("[Status] = 'Active'").HasDatabaseName(ActiveStaffIndex);
        builder.HasIndex(t => t.CurrentVehicleId).IsUnique().HasFilter("[Status] = 'Active'").HasDatabaseName(ActiveVehicleIndex);
        builder.HasIndex(t => new { t.Status, t.NextReminderAtUtc });
        builder.HasIndex(t => t.StartedAtUtc);

        builder.HasOne(t => t.OriginLot).WithMany().HasForeignKey(t => t.OriginLotId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(t => t.ReturnLot).WithMany().HasForeignKey(t => t.ReturnLotId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(t => t.DestinationLocation).WithMany().HasForeignKey(t => t.DestinationLocationId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(t => t.CurrentStaff).WithMany().HasForeignKey(t => t.CurrentStaffId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(t => t.CurrentVehicle).WithMany().HasForeignKey(t => t.CurrentVehicleId).OnDelete(DeleteBehavior.NoAction);
        builder.HasMany(t => t.Segments).WithOne(s => s.Trip).HasForeignKey(s => s.TripId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class TripSegmentConfiguration : IEntityTypeConfiguration<TripSegment>
{
    public void Configure(EntityTypeBuilder<TripSegment> builder)
    {
        builder.ToTable("TripSegments", table =>
            table.HasCheckConstraint("CK_TripSegments_EndedAfterStart", "[EndedAtUtc] IS NULL OR [EndedAtUtc] >= [StartedAtUtc]"));
        builder.Property(s => s.Reason).HasMaxLength(500);
        builder.Property(s => s.SignatureFileName).HasMaxLength(40).IsUnicode(false);

        builder.HasIndex(s => new { s.TripId, s.Seq }).IsUnique();
        builder.HasIndex(s => new { s.StaffId, s.StartedAtUtc });
        builder.HasIndex(s => new { s.VehicleId, s.StartedAtUtc });
        builder.HasIndex(s => s.SignatureFileName).HasFilter("[SignatureFileName] IS NOT NULL");

        builder.HasOne(s => s.Staff).WithMany().HasForeignKey(s => s.StaffId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(s => s.Vehicle).WithMany().HasForeignKey(s => s.VehicleId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(s => s.OutgoingVehicleLot).WithMany().HasForeignKey(s => s.OutgoingVehicleLotId).OnDelete(DeleteBehavior.NoAction);
    }
}
