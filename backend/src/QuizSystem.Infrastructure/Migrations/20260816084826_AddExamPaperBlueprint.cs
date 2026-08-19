using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExamPaperBlueprint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlueprintBloomDistributionJson",
                table: "Exams",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BlueprintCloDistributionJson",
                table: "Exams",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlueprintBloomDistributionJson",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "BlueprintCloDistributionJson",
                table: "Exams");
        }
    }
}
