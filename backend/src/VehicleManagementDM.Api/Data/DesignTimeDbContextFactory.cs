using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VehicleManagementDM.Api.Data;

/// <summary>
/// Used only by "dotnet ef migrations add" on the developer machine. Creating a migration never
/// connects to a database, so a placeholder connection string is enough and no secrets are needed.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=design-time-only;Database=VehicleManagementDM;Integrated Security=true")
            .Options;
        return new AppDbContext(options);
    }
}
