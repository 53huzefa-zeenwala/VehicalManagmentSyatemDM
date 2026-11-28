using VehicleManagementDM.Api.Features.Vehicles;

namespace VehicleManagementDM.Api.Tests.MasterData;

/// <summary>Vehicle history lines for admin changes are readable and never show raw audit JSON (A-10).</summary>
public sealed class VehicleHistoryTextTests
{
    private static readonly Dictionary<int, string> Lots = new() { [3] = "Al Qusais Depot" };

    [Theory]
    [InlineData("Create", """{"id":6,"qrToken":"secret","brand":"Isuzu"}""", "Added by Admin One")]
    [InlineData("StatusChange", """{"status":"UnderMaintenance"}""", "Status set to under maintenance by Admin One")]
    [InlineData("LocationResolve", """{"currentLotId":3}""", "Placed at Al Qusais Depot by Admin One")]
    [InlineData("LocationResolve", """{"currentLotId":99}""", "Location changed by Admin One")]
    [InlineData("Update", """{"brand":"Isuzu","registrationNumber":"D 1"}""", "Edited by Admin One: brand, registration number")]
    [InlineData("Update", null, "Update by Admin One")]
    public void Admin_changes_read_as_sentences(string action, string? afterJson, string expected)
    {
        var text = VehicleHistoryService.DescribeChange(action, "Admin One", afterJson, Lots);

        Assert.Equal(expected, text);
        Assert.DoesNotContain("secret", text);
    }
}
