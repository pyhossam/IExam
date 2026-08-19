using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExamAllowStudentExit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowStudentExit",
                table: "Exams",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowStudentExit",
                table: "Exams");
        }
    }
}
