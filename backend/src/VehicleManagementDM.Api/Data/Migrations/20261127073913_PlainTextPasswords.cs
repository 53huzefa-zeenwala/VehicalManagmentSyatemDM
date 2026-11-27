using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleManagementDM.Api.Data.Migrations
{
    /// <summary>
    /// Passwords become plain text (D-051). Existing hashes can't be turned back into passwords, so the old
    /// column is dropped and existing accounts get an empty password, which never matches a sign-in:
    /// an admin must set each existing account's password again (in SQL, or reset-password).
    /// </summary>
    public partial class PlainTextPasswords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "Users",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Password",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
