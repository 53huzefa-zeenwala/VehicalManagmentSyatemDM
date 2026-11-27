using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleManagementDM.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Trips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Trips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    OriginLotId = table.Column<int>(type: "int", nullable: false),
                    DestinationLocationId = table.Column<int>(type: "int", nullable: false),
                    CurrentStaffId = table.Column<int>(type: "int", nullable: false),
                    CurrentVehicleId = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    ReturnLotId = table.Column<int>(type: "int", nullable: true),
                    CancelCategory = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    CancelNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NextReminderAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    ReminderCount = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trips", x => x.Id);
                    table.CheckConstraint("CK_Trips_EndedAfterStart", "[EndedAtUtc] IS NULL OR [EndedAtUtc] >= [StartedAtUtc]");
                    table.ForeignKey(
                        name: "FK_Trips_Locations_DestinationLocationId",
                        column: x => x.DestinationLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Trips_ParkingLots_OriginLotId",
                        column: x => x.OriginLotId,
                        principalTable: "ParkingLots",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Trips_ParkingLots_ReturnLotId",
                        column: x => x.ReturnLotId,
                        principalTable: "ParkingLots",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Trips_Users_CurrentStaffId",
                        column: x => x.CurrentStaffId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Trips_Vehicles_CurrentVehicleId",
                        column: x => x.CurrentVehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TripSegments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TripId = table.Column<int>(type: "int", nullable: false),
                    Seq = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    StaffId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SignatureFileName = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: true),
                    OutgoingVehicleLotId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripSegments", x => x.Id);
                    table.CheckConstraint("CK_TripSegments_EndedAfterStart", "[EndedAtUtc] IS NULL OR [EndedAtUtc] >= [StartedAtUtc]");
                    table.ForeignKey(
                        name: "FK_TripSegments_ParkingLots_OutgoingVehicleLotId",
                        column: x => x.OutgoingVehicleLotId,
                        principalTable: "ParkingLots",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TripSegments_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TripSegments_Users_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TripSegments_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_DestinationLocationId",
                table: "Trips",
                column: "DestinationLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_OriginLotId",
                table: "Trips",
                column: "OriginLotId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_ReturnLotId",
                table: "Trips",
                column: "ReturnLotId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_StartedAtUtc",
                table: "Trips",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_Status_NextReminderAtUtc",
                table: "Trips",
                columns: new[] { "Status", "NextReminderAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_Trips_ActiveStaff",
                table: "Trips",
                column: "CurrentStaffId",
                unique: true,
                filter: "[Status] = 'Active'");

            migrationBuilder.CreateIndex(
                name: "UX_Trips_ActiveVehicle",
                table: "Trips",
                column: "CurrentVehicleId",
                unique: true,
                filter: "[Status] = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_TripSegments_OutgoingVehicleLotId",
                table: "TripSegments",
                column: "OutgoingVehicleLotId");

            migrationBuilder.CreateIndex(
                name: "IX_TripSegments_SignatureFileName",
                table: "TripSegments",
                column: "SignatureFileName",
                filter: "[SignatureFileName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TripSegments_StaffId_StartedAtUtc",
                table: "TripSegments",
                columns: new[] { "StaffId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TripSegments_TripId_Seq",
                table: "TripSegments",
                columns: new[] { "TripId", "Seq" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TripSegments_VehicleId_StartedAtUtc",
                table: "TripSegments",
                columns: new[] { "VehicleId", "StartedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TripSegments");

            migrationBuilder.DropTable(
                name: "Trips");
        }
    }
}
