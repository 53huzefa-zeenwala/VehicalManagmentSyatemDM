using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleManagementDM.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserAppVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserAppVersions",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    SeenAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAppVersions", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserAppVersions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAppVersions");
        }
    }
}
