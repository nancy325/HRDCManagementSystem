using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRDCManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddMarksObtainedToRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Marks",
                table: "TrainingRegistration",
                newName: "MarksObtained");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MarksObtained",
                table: "TrainingRegistration",
                newName: "Marks");
        }
    }
}
