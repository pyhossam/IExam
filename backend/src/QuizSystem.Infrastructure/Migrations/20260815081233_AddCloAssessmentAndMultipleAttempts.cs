using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCloAssessmentAndMultipleAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attempts_InstitutionId_ExamId_StudentProfileId",
                table: "Attempts");

            migrationBuilder.AddColumn<int>(
                name: "CognitiveLevel",
                table: "Questions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CourseLearningOutcomeId",
                table: "Questions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssessmentType",
                table: "Exams",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxAttempts",
                table: "Exams",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AttemptNumber",
                table: "Attempts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CourseLearningOutcomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Domain = table.Column<int>(type: "INTEGER", nullable: false),
                    CognitiveLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetPercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseLearningOutcomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseLearningOutcomes_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseLearningOutcomes_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_CourseLearningOutcomeId",
                table: "Questions",
                column: "CourseLearningOutcomeId");

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_InstitutionId_ExamId_StudentProfileId_AttemptNumber",
                table: "Attempts",
                columns: new[] { "InstitutionId", "ExamId", "StudentProfileId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseLearningOutcomes_InstitutionId",
                table: "CourseLearningOutcomes",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseLearningOutcomes_InstitutionId_SubjectId_Code",
                table: "CourseLearningOutcomes",
                columns: new[] { "InstitutionId", "SubjectId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseLearningOutcomes_SubjectId",
                table: "CourseLearningOutcomes",
                column: "SubjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_CourseLearningOutcomes_CourseLearningOutcomeId",
                table: "Questions",
                column: "CourseLearningOutcomeId",
                principalTable: "CourseLearningOutcomes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_CourseLearningOutcomes_CourseLearningOutcomeId",
                table: "Questions");

            migrationBuilder.DropTable(
                name: "CourseLearningOutcomes");

            migrationBuilder.DropIndex(
                name: "IX_Questions_CourseLearningOutcomeId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Attempts_InstitutionId_ExamId_StudentProfileId_AttemptNumber",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "CognitiveLevel",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "CourseLearningOutcomeId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "AssessmentType",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "MaxAttempts",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "AttemptNumber",
                table: "Attempts");

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_InstitutionId_ExamId_StudentProfileId",
                table: "Attempts",
                columns: new[] { "InstitutionId", "ExamId", "StudentProfileId" },
                unique: true);
        }
    }
}
