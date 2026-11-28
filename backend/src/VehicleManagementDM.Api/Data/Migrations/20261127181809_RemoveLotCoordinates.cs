using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleManagementDM.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLotCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ParkingLots_Latitude",
                table: "ParkingLots");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ParkingLots_Longitude",
                table: "ParkingLots");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "ParkingLots");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "ParkingLots");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "ParkingLots",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "ParkingLots",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ParkingLots_Latitude",
                table: "ParkingLots",
                sql: "[Latitude] BETWEEN -90 AND 90");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ParkingLots_Longitude",
                table: "ParkingLots",
                sql: "[Longitude] BETWEEN -180 AND 180");
        }
    }
}
