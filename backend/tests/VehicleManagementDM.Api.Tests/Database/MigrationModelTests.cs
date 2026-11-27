#pragma warning disable EF1001 // Internal EF Core API: the only way to compare model and snapshot on EF Core 8.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using VehicleManagementDM.Api.Data;

namespace VehicleManagementDM.Api.Tests.Database;

/// <summary>Fails when the C# model was changed without adding a migration (D-030, ARCHITECTURE.md §3).</summary>
public sealed class MigrationModelTests
{
    [Fact]
    public void Model_has_no_changes_without_a_migration()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=true")
            .Options;
        using var context = new AppDbContext(options);

        var snapshot = context.GetService<IMigrationsAssembly>().ModelSnapshot?.Model;
        Assert.NotNull(snapshot);
        if (snapshot is IMutableModel mutable)
        {
            snapshot = mutable.FinalizeModel();
        }

        snapshot = context.GetService<IModelRuntimeInitializer>().Initialize(snapshot);
        var differences = context.GetService<IMigrationsModelDiffer>().GetDifferences(
            snapshot.GetRelationalModel(),
            context.GetService<IDesignTimeModel>().Model.GetRelationalModel());

        Assert.True(
            differences.Count == 0,
            "The model has changes without a migration. Run: dotnet ef migrations add <Name> --project src/VehicleManagementDM.Api --output-dir Data/Migrations. " +
            $"Differences: {string.Join(", ", differences.Select(d => d.GetType().Name))}");
    }
}
