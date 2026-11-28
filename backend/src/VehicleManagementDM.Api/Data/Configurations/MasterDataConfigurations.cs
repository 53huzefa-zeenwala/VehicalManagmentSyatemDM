using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleManagementDM.Api.Domain.Entities;

namespace VehicleManagementDM.Api.Data.Configurations;

internal sealed class VehicleCategoryConfiguration : IEntityTypeConfiguration<VehicleCategory>
{
    public const string NameIndex = "UX_VehicleCategories_Name";

    public void Configure(EntityTypeBuilder<VehicleCategory> builder)
    {
        builder.ToTable("VehicleCategories");
        builder.Property(c => c.Name).HasMaxLength(60);
        builder.HasIndex(c => c.Name).IsUnique().HasDatabaseName(NameIndex);
        builder.HasMany(c => c.Types).WithOne(t => t.Category).HasForeignKey(t => t.CategoryId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class VehicleTypeConfiguration : IEntityTypeConfiguration<VehicleType>
{
    public const string NameIndex = "UX_VehicleTypes_CategoryId_Name";

    public void Configure(EntityTypeBuilder<VehicleType> builder)
    {
        builder.ToTable("VehicleTypes");
        builder.Property(t => t.Name).HasMaxLength(60);
        builder.HasIndex(t => new { t.CategoryId, t.Name }).IsUnique().HasDatabaseName(NameIndex);
    }
}

internal sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public const string RegistrationIndex = "UX_Vehicles_RegistrationKey";
    public const string QrTokenIndex = "UX_Vehicles_QrToken";

    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");
        builder.Property(v => v.RegistrationNumber).HasMaxLength(30);
        builder.Property(v => v.RegistrationKey).HasMaxLength(30).IsUnicode(false);
        builder.Property(v => v.Brand).HasMaxLength(60);
        builder.Property(v => v.Model).HasMaxLength(60);
        builder.Property(v => v.Colour).HasMaxLength(30);
        builder.Property(v => v.PhotoFileName).HasMaxLength(40).IsUnicode(false);
        builder.Property(v => v.QrToken).HasMaxLength(22).IsUnicode(false).IsFixedLength();
        builder.Property(v => v.RowVersion).IsRowVersion();

        builder.HasIndex(v => v.RegistrationKey).IsUnique().HasDatabaseName(RegistrationIndex);
        builder.HasIndex(v => v.QrToken).IsUnique().HasDatabaseName(QrTokenIndex);
        builder.HasIndex(v => new { v.CurrentLotId, v.VehicleTypeId });
        builder.HasIndex(v => v.Status);

        builder.HasOne(v => v.VehicleType).WithMany().HasForeignKey(v => v.VehicleTypeId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(v => v.CurrentLot).WithMany().HasForeignKey(v => v.CurrentLotId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class ParkingLotConfiguration : IEntityTypeConfiguration<ParkingLot>
{
    public const string CodeIndex = "UX_ParkingLots_Code";

    public void Configure(EntityTypeBuilder<ParkingLot> builder)
    {
        builder.ToTable("ParkingLots", table =>
        {
            table.HasCheckConstraint("CK_ParkingLots_TotalCapacity", "[TotalCapacity] >= 0");
            table.HasCheckConstraint("CK_ParkingLots_BaselineOccupied", "[BaselineOccupied] >= 0");
        });
        builder.Property(l => l.Code).HasMaxLength(20).IsUnicode(false);
        builder.Property(l => l.Name).HasMaxLength(100);
        builder.Property(l => l.Address).HasMaxLength(300);
        builder.Property(l => l.ContactName).HasMaxLength(100);
        builder.Property(l => l.ContactNumber).HasMaxLength(20).IsUnicode(false);
        builder.Property(l => l.OpensAt).HasPrecision(0);
        builder.Property(l => l.ClosesAt).HasPrecision(0);
        builder.Property(l => l.RowVersion).IsRowVersion();

        builder.HasIndex(l => l.Code).IsUnique().HasDatabaseName(CodeIndex);
        builder.HasMany(l => l.TypeCapacities).WithOne().HasForeignKey(c => c.LotId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class LotTypeCapacityConfiguration : IEntityTypeConfiguration<LotTypeCapacity>
{
    public void Configure(EntityTypeBuilder<LotTypeCapacity> builder)
    {
        builder.ToTable("LotTypeCapacities", table =>
        {
            table.HasCheckConstraint("CK_LotTypeCapacities_Capacity", "[Capacity] >= 0");
            table.HasCheckConstraint("CK_LotTypeCapacities_BaselineOccupied", "[BaselineOccupied] >= 0");
        });
        builder.HasKey(c => new { c.LotId, c.VehicleTypeId });
        builder.HasOne(c => c.VehicleType).WithMany().HasForeignKey(c => c.VehicleTypeId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class AreaConfiguration : IEntityTypeConfiguration<Area>
{
    public const string NameIndex = "UX_Areas_Name";

    public void Configure(EntityTypeBuilder<Area> builder)
    {
        builder.ToTable("Areas");
        builder.Property(a => a.Name).HasMaxLength(100);
        builder.HasIndex(a => a.Name).IsUnique().HasDatabaseName(NameIndex);
        builder.HasMany(a => a.Locations).WithOne(l => l.Area).HasForeignKey(l => l.AreaId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public const string NameIndex = "UX_Locations_AreaId_Name";

    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations");
        builder.Property(l => l.Name).HasMaxLength(150);
        builder.HasIndex(l => new { l.AreaId, l.Name }).IsUnique().HasDatabaseName(NameIndex);
    }
}

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.Property(a => a.Action).HasMaxLength(40).IsUnicode(false);
        builder.Property(a => a.EntityType).HasMaxLength(40).IsUnicode(false);
        builder.HasIndex(a => a.AtUtc);
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => new { a.EntityType, a.EntityId });
        builder.HasOne<User>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.NoAction);
    }
}
