using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleManagementDM.Api.Domain.Entities;

namespace VehicleManagementDM.Api.Data.Configurations;

internal sealed class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    /// <summary>One open (pending or in-transit) transfer per vehicle.</summary>
    public const string OpenVehicleIndex = "UX_Transfers_OpenVehicle";

    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("Transfers", table =>
            table.HasCheckConstraint("CK_Transfers_DifferentLots", "[FromLotId] <> [ToLotId]"));
        builder.Property(t => t.Note).HasMaxLength(500);
        builder.Property(t => t.RowVersion).IsRowVersion();

        builder.HasIndex(t => t.VehicleId).IsUnique()
            .HasFilter("[Status] IN ('PendingDeparture', 'InTransit')")
            .HasDatabaseName(OpenVehicleIndex);
        builder.HasIndex(t => t.DepartedByUserId).HasFilter("[Status] = 'InTransit'");
        builder.HasIndex(t => new { t.Status, t.NextReminderAtUtc });
        builder.HasIndex(t => t.CreatedAtUtc);

        builder.HasOne(t => t.Vehicle).WithMany().HasForeignKey(t => t.VehicleId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(t => t.FromLot).WithMany().HasForeignKey(t => t.FromLotId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(t => t.ToLot).WithMany().HasForeignKey(t => t.ToLotId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(t => t.CreatedBy).WithMany().HasForeignKey(t => t.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(t => t.DepartedBy).WithMany().HasForeignKey(t => t.DepartedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(t => t.ArrivedBy).WithMany().HasForeignKey(t => t.ArrivedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(t => t.CancelledBy).WithMany().HasForeignKey(t => t.CancelledByUserId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class ReminderLogConfiguration : IEntityTypeConfiguration<ReminderLog>
{
    public void Configure(EntityTypeBuilder<ReminderLog> builder)
    {
        builder.ToTable("ReminderLogs", table => table.HasCheckConstraint(
            "CK_ReminderLogs_OneTarget",
            "(CASE WHEN [TripId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [TransferId] IS NULL THEN 0 ELSE 1 END) = 1"));
        builder.HasIndex(r => new { r.TripId, r.ReminderNumber });
        builder.HasIndex(r => new { r.TransferId, r.ReminderNumber });
        builder.HasOne<Trip>().WithMany().HasForeignKey(r => r.TripId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Transfer>().WithMany().HasForeignKey(r => r.TransferId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.RecipientUserId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.Property(n => n.Type).HasMaxLength(40);
        builder.Property(n => n.Title).HasMaxLength(120);
        builder.Property(n => n.Body).HasMaxLength(500);
        builder.Property(n => n.TargetType).HasMaxLength(30).IsUnicode(false);
        builder.HasIndex(n => new { n.UserId, n.ReadAtUtc, n.CreatedAtUtc });
        builder.HasOne<User>().WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.NoAction);
    }
}
