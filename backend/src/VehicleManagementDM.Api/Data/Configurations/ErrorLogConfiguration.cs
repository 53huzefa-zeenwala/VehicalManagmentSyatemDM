using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleManagementDM.Api.Domain.Entities;

namespace VehicleManagementDM.Api.Data.Configurations;

internal sealed class ErrorLogConfiguration : IEntityTypeConfiguration<ErrorLog>
{
    public void Configure(EntityTypeBuilder<ErrorLog> builder)
    {
        builder.ToTable("ErrorLogs");

        builder.Property(e => e.TraceId).HasMaxLength(64).IsUnicode(false);
        builder.Property(e => e.Level).HasMaxLength(20).IsUnicode(false);
        builder.Property(e => e.Code).HasMaxLength(50).IsUnicode(false);
        builder.Property(e => e.Method).HasMaxLength(10).IsUnicode(false);
        builder.Property(e => e.Path).HasMaxLength(400);
        builder.Property(e => e.Role).HasMaxLength(30).IsUnicode(false);
        builder.Property(e => e.ExceptionType).HasMaxLength(300);
        builder.Property(e => e.Message).HasMaxLength(2000);
        builder.Property(e => e.ClientIp).HasMaxLength(45).IsUnicode(false);
        builder.Property(e => e.AppVersion).HasMaxLength(30).IsUnicode(false);

        builder.HasIndex(e => e.AtUtc);
        builder.HasIndex(e => e.TraceId);
    }
}
