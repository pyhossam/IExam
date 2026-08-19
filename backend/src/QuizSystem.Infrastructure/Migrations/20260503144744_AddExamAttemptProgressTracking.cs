using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExamAttemptProgressTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExamAttemptDraftAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExamAttemptId = table.Column<Guid>(type: "TEXT", nullable: false),
                    QuestionSnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SelectedAnswer = table.Column<string>(type: "TEXT", nullable: true),
                    SavedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAttemptDraftAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamAttemptDraftAnswers_Attempts_ExamAttemptId",
                        column: x => x.ExamAttemptId,
                        principalTable: "Attempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamAttemptViolations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExamAttemptId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Details = table.Column<string>(type: "TEXT", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAttemptViolations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamAttemptViolations_Attempts_ExamAttemptId",
                        column: x => x.ExamAttemptId,
                        principalTable: "Attempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttemptDraftAnswers_ExamAttemptId",
                table: "ExamAttemptDraftAnswers",
                column: "ExamAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttemptViolations_ExamAttemptId",
                table: "ExamAttemptViolations",
                column: "ExamAttemptId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamAttemptDraftAnswers");

            migrationBuilder.DropTable(
                name: "ExamAttemptViolations");
        }
    }
}
