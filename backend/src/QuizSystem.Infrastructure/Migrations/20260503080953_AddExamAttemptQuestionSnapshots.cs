using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExamAttemptQuestionSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExamAttemptQuestionSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExamAttemptId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OriginalQuestionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    QuestionText = table.Column<string>(type: "TEXT", nullable: false),
                    QuestionImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    ChoiceADisplayLabel = table.Column<string>(type: "TEXT", nullable: false),
                    ChoiceAOriginalKey = table.Column<string>(type: "TEXT", nullable: false),
                    ChoiceAText = table.Column<string>(type: "TEXT", nullable: false),
                    ChoiceAImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    ChoiceBDisplayLabel = table.Column<string>(type: "TEXT", nullable: false),
                    ChoiceBOriginalKey = table.Column<string>(type: "TEXT", nullable: false),
                    ChoiceBText = table.Column<string>(type: "TEXT", nullable: false),
                    ChoiceBImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    ChoiceCDisplayLabel = table.Column<string>(type: "TEXT", nullable: false),
                    ChoiceCOriginalKey = table.Column<string>(type: "TEXT", nullable: false),
                    ChoiceCText = table.Column<string>(type: "TEXT", nullable: false),
                    ChoiceCImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    ChoiceDDisplayLabel = table.Column<string>(type: "TEXT", nullable: false),
                    ChoiceDOriginalKey = table.Column<string>(type: "TEXT", nullable: false),
                    ChoiceDText = table.Column<string>(type: "TEXT", nullable: false),
                    ChoiceDImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    CorrectOriginalKey = table.Column<string>(type: "TEXT", nullable: false),
                    Explanation = table.Column<string>(type: "TEXT", nullable: true),
                    SelectedOriginalKey = table.Column<string>(type: "TEXT", nullable: true),
                    IsCorrect = table.Column<bool>(type: "INTEGER", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAttemptQuestionSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamAttemptQuestionSnapshots_Attempts_ExamAttemptId",
                        column: x => x.ExamAttemptId,
                        principalTable: "Attempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttemptQuestionSnapshots_ExamAttemptId",
                table: "ExamAttemptQuestionSnapshots",
                column: "ExamAttemptId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamAttemptQuestionSnapshots");
        }
    }
}
