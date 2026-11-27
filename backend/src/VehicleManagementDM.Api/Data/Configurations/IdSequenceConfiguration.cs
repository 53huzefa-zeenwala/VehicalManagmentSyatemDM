using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleManagementDM.Api.Domain.Entities;
using VehicleManagementDM.Api.Domain.Enums;

namespace VehicleManagementDM.Api.Data.Configurations;

internal sealed class IdSequenceConfiguration : IEntityTypeConfiguration<IdSequence>
{
    public void Configure(EntityTypeBuilder<IdSequence> builder)
    {
        builder.ToTable("IdSequences");
        builder.HasKey(s => s.Role);

        builder.HasData(
            new IdSequence { Role = UserRole.Staff, LastNumber = 0 },
            new IdSequence { Role = UserRole.Admin, LastNumber = 0 });
    }
}
