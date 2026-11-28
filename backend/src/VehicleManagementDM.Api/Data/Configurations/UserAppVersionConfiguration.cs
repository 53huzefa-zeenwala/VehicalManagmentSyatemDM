using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleManagementDM.Api.Domain.Entities;

namespace VehicleManagementDM.Api.Data.Configurations;

internal sealed class UserAppVersionConfiguration : IEntityTypeConfiguration<UserAppVersion>
{
    public void Configure(EntityTypeBuilder<UserAppVersion> builder)
    {
        builder.ToTable("UserAppVersions");
        builder.HasKey(v => v.UserId);
        builder.Property(v => v.UserId).ValueGeneratedNever();
        builder.Property(v => v.Version).HasMaxLength(20).IsUnicode(false);
        builder.HasOne(v => v.User).WithOne().HasForeignKey<UserAppVersion>(v => v.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
