using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleManagementDM.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AppMinimumVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MinAdminAppVersion",
                table: "AppSettings",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MinStaffAppVersion",
                table: "AppSettings",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "MinAdminAppVersion", "MinStaffAppVersion" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinAdminAppVersion",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "MinStaffAppVersion",
                table: "AppSettings");
        }
    }
}
