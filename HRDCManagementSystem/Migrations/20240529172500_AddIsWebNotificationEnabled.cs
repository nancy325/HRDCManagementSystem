using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRDCManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddIsWebNotificationEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsWebNotificationEnabled",
                table: "UserMasters",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsWebNotificationEnabled",
                table: "UserMasters");
        }
    }
}