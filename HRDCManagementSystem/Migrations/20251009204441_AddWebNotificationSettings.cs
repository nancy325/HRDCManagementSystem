using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRDCManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddWebNotificationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__Feedback__Questi__787EE5A0",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "TrainerRating",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "TrainingRating",
                table: "Feedback");

            migrationBuilder.AddColumn<bool>(
                name: "IsWebNotificationEnabled",
                table: "UserMaster",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "QuestionText",
                table: "FeedbackQuestion",
                type: "varchar(255)",
                unicode: false,
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldUnicode: false,
                oldMaxLength: 255);

            migrationBuilder.AddColumn<bool>(
                name: "IsCommon",
                table: "FeedbackQuestion",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "QuestionType",
                table: "FeedbackQuestion",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                defaultValue: "Rating");

            migrationBuilder.AddColumn<int>(
                name: "TrainingSysID",
                table: "FeedbackQuestion",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "QuestionID",
                table: "Feedback",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RatingValue",
                table: "Feedback",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseText",
                table: "Feedback",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    NotificationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserSysID = table.Column<int>(type: "int", nullable: true),
                    UserType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()"),
                    CreateUserId = table.Column<int>(type: "int", nullable: true),
                    CreateDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    RecStatus = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false, defaultValue: "active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.NotificationID);
                    table.ForeignKey(
                        name: "FK_Notification_User",
                        column: x => x.UserSysID,
                        principalTable: "UserMaster",
                        principalColumn: "UserSysID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notification_UserSysID",
                table: "Notification",
                column: "UserSysID");

            migrationBuilder.AddForeignKey(
                name: "FK__Feedback__Questi__787EE5A0",
                table: "Feedback",
                column: "QuestionID",
                principalTable: "FeedbackQuestion",
                principalColumn: "QuestionID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__Feedback__Questi__787EE5A0",
                table: "Feedback");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropColumn(
                name: "IsWebNotificationEnabled",
                table: "UserMaster");

            migrationBuilder.DropColumn(
                name: "IsCommon",
                table: "FeedbackQuestion");

            migrationBuilder.DropColumn(
                name: "QuestionType",
                table: "FeedbackQuestion");

            migrationBuilder.DropColumn(
                name: "TrainingSysID",
                table: "FeedbackQuestion");

            migrationBuilder.DropColumn(
                name: "RatingValue",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "ResponseText",
                table: "Feedback");

            migrationBuilder.AlterColumn<string>(
                name: "QuestionText",
                table: "FeedbackQuestion",
                type: "varchar(255)",
                unicode: false,
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldUnicode: false,
                oldMaxLength: 255,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "QuestionID",
                table: "Feedback",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "Feedback",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrainerRating",
                table: "Feedback",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrainingRating",
                table: "Feedback",
                type: "int",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK__Feedback__Questi__787EE5A0",
                table: "Feedback",
                column: "QuestionID",
                principalTable: "FeedbackQuestion",
                principalColumn: "QuestionID");
        }
    }
}
