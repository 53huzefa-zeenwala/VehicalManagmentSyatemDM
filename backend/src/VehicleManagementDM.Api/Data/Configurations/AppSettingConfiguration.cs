using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleManagementDM.Api.Domain.Entities;

namespace VehicleManagementDM.Api.Data.Configurations;

internal sealed class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.ToTable("AppSettings", table =>
        {
            table.HasCheckConstraint("CK_AppSettings_Singleton", $"[Id] = {AppSetting.SingletonId}");
            table.HasCheckConstraint("CK_AppSettings_ReminderDelay", "[ReminderDelayMinutes] > 0");
            table.HasCheckConstraint("CK_AppSettings_ReminderRepeat", "[ReminderRepeatMinutes] > 0");
        });

        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.StaffIdPrefix).HasMaxLength(5).IsUnicode(false);
        builder.Property(s => s.MinStaffAppVersion).HasMaxLength(20).IsUnicode(false);
        builder.Property(s => s.MinAdminAppVersion).HasMaxLength(20).IsUnicode(false);
        builder.Property(s => s.RowVersion).IsRowVersion();

        builder.HasData(new AppSetting
        {
            Id = AppSetting.SingletonId,
            StaffIdPrefix = "DM",
            ReminderDelayMinutes = 120,
            ReminderRepeatMinutes = 30,
        });
    }
}
