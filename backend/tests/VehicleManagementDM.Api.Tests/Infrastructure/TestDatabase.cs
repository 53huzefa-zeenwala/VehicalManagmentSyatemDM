using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace VehicleManagementDM.Api.Tests.Infrastructure;

/// <summary>
/// Throwaway test databases on the dev/test SQL Server (D-038). The server connection comes from
/// <c>TestDatabase:ConnectionString</c> (user-secrets of this project, or the environment variable
/// <c>TestDatabase__ConnectionString</c>); it is never stored in git.
/// Only databases named <c>VehicleManagementDM_Test_&lt;guid&gt;</c> are ever dropped.
/// </summary>
public static partial class TestDatabase
{
    private const string NamePrefix = "VehicleManagementDM_Test_";

    private static readonly Lazy<string> ServerConnectionString = new(() =>
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(TestDatabase).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();
        return configuration["TestDatabase:ConnectionString"] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException(
                "TestDatabase:ConnectionString is not set. Set it with 'dotnet user-secrets set TestDatabase:ConnectionString \"...\" " +
                "--project tests/VehicleManagementDM.Api.Tests' or the TestDatabase__ConnectionString environment variable.");
    });

    public static string NewName() => NamePrefix + Guid.NewGuid().ToString("N");

    public static string ConnectionStringFor(string databaseName) =>
        new SqlConnectionStringBuilder(ServerConnectionString.Value) { InitialCatalog = databaseName }.ConnectionString;

    public static async Task DropAsync(string databaseName)
    {
        if (!TestNamePattern().IsMatch(databaseName))
        {
            throw new InvalidOperationException($"Refusing to drop '{databaseName}': not a test database.");
        }

        SqlConnection.ClearAllPools();
        await using var connection = new SqlConnection(ConnectionStringFor("master"));
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"IF DB_ID(N'{databaseName}') IS NOT NULL BEGIN " +
            $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
            $"DROP DATABASE [{databaseName}]; END";
        await command.ExecuteNonQueryAsync();
    }

    [GeneratedRegex("^VehicleManagementDM_Test_[0-9a-f]{32}$")]
    private static partial Regex TestNamePattern();
}
