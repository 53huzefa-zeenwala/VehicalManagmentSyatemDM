using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleManagementDM.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TransfersAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TargetType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    TargetId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Transfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    FromLotId = table.Column<int>(type: "int", nullable: false),
                    ToLotId = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    DepartedByUserId = table.Column<int>(type: "int", nullable: true),
                    DepartedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    ArrivedByUserId = table.Column<int>(type: "int", nullable: true),
                    ArrivedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    CancelledByUserId = table.Column<int>(type: "int", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    NextReminderAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    ReminderCount = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transfers", x => x.Id);
                    table.CheckConstraint("CK_Transfers_DifferentLots", "[FromLotId] <> [ToLotId]");
                    table.ForeignKey(
                        name: "FK_Transfers_ParkingLots_FromLotId",
                        column: x => x.FromLotId,
                        principalTable: "ParkingLots",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Transfers_ParkingLots_ToLotId",
                        column: x => x.ToLotId,
                        principalTable: "ParkingLots",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Transfers_Users_ArrivedByUserId",
                        column: x => x.ArrivedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Transfers_Users_CancelledByUserId",
                        column: x => x.CancelledByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Transfers_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Transfers_Users_DepartedByUserId",
                        column: x => x.DepartedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Transfers_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReminderLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TripId = table.Column<int>(type: "int", nullable: true),
                    TransferId = table.Column<int>(type: "int", nullable: true),
                    RecipientUserId = table.Column<int>(type: "int", nullable: false),
                    ReminderNumber = table.Column<int>(type: "int", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReminderLogs", x => x.Id);
                    table.CheckConstraint("CK_ReminderLogs_OneTarget", "(CASE WHEN [TripId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [TransferId] IS NULL THEN 0 ELSE 1 END) = 1");
                    table.ForeignKey(
                        name: "FK_ReminderLogs_Transfers_TransferId",
                        column: x => x.TransferId,
                        principalTable: "Transfers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReminderLogs_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReminderLogs_Users_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_ReadAtUtc_CreatedAtUtc",
                table: "Notifications",
                columns: new[] { "UserId", "ReadAtUtc", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReminderLogs_RecipientUserId",
                table: "ReminderLogs",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReminderLogs_TransferId_ReminderNumber",
                table: "ReminderLogs",
                columns: new[] { "TransferId", "ReminderNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_ReminderLogs_TripId_ReminderNumber",
                table: "ReminderLogs",
                columns: new[] { "TripId", "ReminderNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_ArrivedByUserId",
                table: "Transfers",
                column: "ArrivedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_CancelledByUserId",
                table: "Transfers",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_CreatedAtUtc",
                table: "Transfers",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_CreatedByUserId",
                table: "Transfers",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_DepartedByUserId",
                table: "Transfers",
                column: "DepartedByUserId",
                filter: "[Status] = 'InTransit'");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_FromLotId",
                table: "Transfers",
                column: "FromLotId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_Status_NextReminderAtUtc",
                table: "Transfers",
                columns: new[] { "Status", "NextReminderAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_ToLotId",
                table: "Transfers",
                column: "ToLotId");

            migrationBuilder.CreateIndex(
                name: "UX_Transfers_OpenVehicle",
                table: "Transfers",
                column: "VehicleId",
                unique: true,
                filter: "[Status] IN ('PendingDeparture', 'InTransit')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "ReminderLogs");

            migrationBuilder.DropTable(
                name: "Transfers");
        }
    }
}
