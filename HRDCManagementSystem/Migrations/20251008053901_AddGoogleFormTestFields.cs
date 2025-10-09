using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRDCManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleFormTestFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoogleFormTestLink",
                table: "TrainingProgram",
                type: "varchar(500)",
                unicode: false,
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TestInstructions",
                table: "TrainingProgram",
                type: "varchar(1000)",
                unicode: false,
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "TestAvailableFrom",
                table: "TrainingProgram",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "TestAvailableUntil",
                table: "TrainingProgram",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoogleFormTestLink",
                table: "TrainingProgram");

            migrationBuilder.DropColumn(
                name: "TestInstructions",
                table: "TrainingProgram");

            migrationBuilder.DropColumn(
                name: "TestAvailableFrom",
                table: "TrainingProgram");

            migrationBuilder.DropColumn(
                name: "TestAvailableUntil",
                table: "TrainingProgram");
        }
    }
}
