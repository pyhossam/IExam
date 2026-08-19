using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionImageUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChoiceAImageUrl",
                table: "Questions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChoiceBImageUrl",
                table: "Questions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChoiceCImageUrl",
                table: "Questions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChoiceDImageUrl",
                table: "Questions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuestionImageUrl",
                table: "Questions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChoiceAImageUrl",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ChoiceBImageUrl",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ChoiceCImageUrl",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ChoiceDImageUrl",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "QuestionImageUrl",
                table: "Questions");
        }
    }
}
