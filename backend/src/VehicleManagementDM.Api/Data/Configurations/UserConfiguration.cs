using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleManagementDM.Api.Domain.Entities;

namespace VehicleManagementDM.Api.Data.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public const string LoginIdIndex = "UX_Users_LoginId";

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", table =>
        {
            table.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Admin', 'Staff')");
            table.HasCheckConstraint("CK_Users_StaffLicence", "[Role] <> 'Staff' OR [LicenceNumber] IS NOT NULL");
        });

        builder.Property(u => u.LoginId).HasMaxLength(20).IsUnicode(false);
        builder.Property(u => u.FullName).HasMaxLength(100);
        builder.Property(u => u.Mobile).HasMaxLength(20).IsUnicode(false);
        builder.Property(u => u.Email).HasMaxLength(256);
        builder.Property(u => u.LicenceNumber).HasMaxLength(50);
        builder.Property(u => u.PhotoFileName).HasMaxLength(40).IsUnicode(false);
        builder.Property(u => u.PasswordHash).HasMaxLength(200).IsUnicode(false);
        builder.Property(u => u.SecurityStamp).HasMaxLength(32).IsUnicode(false).IsFixedLength();
        builder.Property(u => u.RowVersion).IsRowVersion();

        builder.HasIndex(u => u.LoginId).IsUnique().HasDatabaseName(LoginIdIndex);
    }
}
