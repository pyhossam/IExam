using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncCurrentModelAfterSuperAdminFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_StudentCode",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_ExamId_StudentProfileId",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_ParentStudentLinks_ParentProfileId_StudentProfileId",
                table: "ParentStudentLinks");

            migrationBuilder.DropIndex(
                name: "IX_Parents_ParentCode",
                table: "Parents");

            migrationBuilder.DropIndex(
                name: "IX_Exams_ExamCode",
                table: "Exams");

            migrationBuilder.DropIndex(
                name: "IX_Attempts_ExamId_StudentProfileId",
                table: "Attempts");

            migrationBuilder.AddColumn<Guid>(
                name: "TeacherProfileId",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Students",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "GradeLevelId",
                table: "Students",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InstitutionId",
                table: "Students",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Students",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClassSectionId",
                table: "Registrations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InstitutionId",
                table: "Registrations",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "InstitutionId",
                table: "Questions",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "InstitutionId",
                table: "ParentStudentLinks",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "InstitutionId",
                table: "Parents",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "ExamManagementMode",
                table: "Institutions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ClassSectionId",
                table: "Exams",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InstitutionId",
                table: "Exams",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SubjectId",
                table: "Exams",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TeacherProfileId",
                table: "Exams",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InstitutionId",
                table: "ExamAttemptViolations",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "InstitutionId",
                table: "ExamAttemptQuestionSnapshots",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "InstitutionId",
                table: "ExamAttemptDraftAnswers",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClassSectionId",
                table: "Attempts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InstitutionId",
                table: "Attempts",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "InstitutionId",
                table: "AttemptAnswers",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("""
                INSERT INTO Institutions (Id, Name, Type, Address, PhoneNumber, Email, LogoUrl, IsActive, CreatedAtUtc, ExamManagementMode)
                SELECT '00000000-0000-0000-0000-000000000000', 'Default Institution', NULL, NULL, NULL, NULL, NULL, 1, CURRENT_TIMESTAMP, 0
                WHERE NOT EXISTS (SELECT 1 FROM Institutions);

                UPDATE Students
                SET InstitutionId = COALESCE((SELECT Id FROM Institutions WHERE Id <> '00000000-0000-0000-0000-000000000000' ORDER BY CreatedAtUtc LIMIT 1), '00000000-0000-0000-0000-000000000000')
                WHERE InstitutionId = '00000000-0000-0000-0000-000000000000';

                UPDATE Registrations
                SET InstitutionId = COALESCE((SELECT Id FROM Institutions WHERE Id <> '00000000-0000-0000-0000-000000000000' ORDER BY CreatedAtUtc LIMIT 1), '00000000-0000-0000-0000-000000000000')
                WHERE InstitutionId = '00000000-0000-0000-0000-000000000000';

                UPDATE Questions
                SET InstitutionId = COALESCE((SELECT Id FROM Institutions WHERE Id <> '00000000-0000-0000-0000-000000000000' ORDER BY CreatedAtUtc LIMIT 1), '00000000-0000-0000-0000-000000000000')
                WHERE InstitutionId = '00000000-0000-0000-0000-000000000000';

                UPDATE ParentStudentLinks
                SET InstitutionId = COALESCE((SELECT Id FROM Institutions WHERE Id <> '00000000-0000-0000-0000-000000000000' ORDER BY CreatedAtUtc LIMIT 1), '00000000-0000-0000-0000-000000000000')
                WHERE InstitutionId = '00000000-0000-0000-0000-000000000000';

                UPDATE Parents
                SET InstitutionId = COALESCE((SELECT Id FROM Institutions WHERE Id <> '00000000-0000-0000-0000-000000000000' ORDER BY CreatedAtUtc LIMIT 1), '00000000-0000-0000-0000-000000000000')
                WHERE InstitutionId = '00000000-0000-0000-0000-000000000000';

                UPDATE Exams
                SET InstitutionId = COALESCE((SELECT Id FROM Institutions WHERE Id <> '00000000-0000-0000-0000-000000000000' ORDER BY CreatedAtUtc LIMIT 1), '00000000-0000-0000-0000-000000000000')
                WHERE InstitutionId = '00000000-0000-0000-0000-000000000000';

                UPDATE ExamAttemptViolations
                SET InstitutionId = COALESCE((SELECT Id FROM Institutions WHERE Id <> '00000000-0000-0000-0000-000000000000' ORDER BY CreatedAtUtc LIMIT 1), '00000000-0000-0000-0000-000000000000')
                WHERE InstitutionId = '00000000-0000-0000-0000-000000000000';

                UPDATE ExamAttemptQuestionSnapshots
                SET InstitutionId = COALESCE((SELECT Id FROM Institutions WHERE Id <> '00000000-0000-0000-0000-000000000000' ORDER BY CreatedAtUtc LIMIT 1), '00000000-0000-0000-0000-000000000000')
                WHERE InstitutionId = '00000000-0000-0000-0000-000000000000';

                UPDATE ExamAttemptDraftAnswers
                SET InstitutionId = COALESCE((SELECT Id FROM Institutions WHERE Id <> '00000000-0000-0000-0000-000000000000' ORDER BY CreatedAtUtc LIMIT 1), '00000000-0000-0000-0000-000000000000')
                WHERE InstitutionId = '00000000-0000-0000-0000-000000000000';

                UPDATE Attempts
                SET InstitutionId = COALESCE((SELECT Id FROM Institutions WHERE Id <> '00000000-0000-0000-0000-000000000000' ORDER BY CreatedAtUtc LIMIT 1), '00000000-0000-0000-0000-000000000000')
                WHERE InstitutionId = '00000000-0000-0000-0000-000000000000';

                UPDATE AttemptAnswers
                SET InstitutionId = COALESCE((SELECT Id FROM Institutions WHERE Id <> '00000000-0000-0000-0000-000000000000' ORDER BY CreatedAtUtc LIMIT 1), '00000000-0000-0000-0000-000000000000')
                WHERE InstitutionId = '00000000-0000-0000-0000-000000000000';
                """);

            migrationBuilder.CreateTable(
                name: "GradeLevels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradeLevels_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Teachers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    TeacherCode = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teachers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teachers_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GradeLevelId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subjects_GradeLevels_GradeLevelId",
                        column: x => x.GradeLevelId,
                        principalTable: "GradeLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subjects_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClassSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GradeLevelId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TeacherProfileId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    GenderType = table.Column<string>(type: "TEXT", nullable: false),
                    AcademicYear = table.Column<string>(type: "TEXT", nullable: false),
                    Term = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassSections_GradeLevels_GradeLevelId",
                        column: x => x.GradeLevelId,
                        principalTable: "GradeLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassSections_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassSections_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassSections_Teachers_TeacherProfileId",
                        column: x => x.TeacherProfileId,
                        principalTable: "Teachers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SectionStudents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClassSectionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectionStudents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SectionStudents_ClassSections_ClassSectionId",
                        column: x => x.ClassSectionId,
                        principalTable: "ClassSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SectionStudents_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SectionStudents_Students_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_TeacherProfileId",
                table: "Users",
                column: "TeacherProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_GradeLevelId",
                table: "Students",
                column: "GradeLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_InstitutionId_StudentCode",
                table: "Students",
                columns: new[] { "InstitutionId", "StudentCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_ClassSectionId",
                table: "Registrations",
                column: "ClassSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_ExamId",
                table: "Registrations",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_InstitutionId_ExamId_StudentProfileId",
                table: "Registrations",
                columns: new[] { "InstitutionId", "ExamId", "StudentProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_InstitutionId",
                table: "Questions",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentStudentLinks_InstitutionId_ParentProfileId_StudentProfileId",
                table: "ParentStudentLinks",
                columns: new[] { "InstitutionId", "ParentProfileId", "StudentProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentStudentLinks_ParentProfileId",
                table: "ParentStudentLinks",
                column: "ParentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Parents_InstitutionId_ParentCode",
                table: "Parents",
                columns: new[] { "InstitutionId", "ParentCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Institutions_Name",
                table: "Institutions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_ClassSectionId",
                table: "Exams",
                column: "ClassSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_InstitutionId_ExamCode",
                table: "Exams",
                columns: new[] { "InstitutionId", "ExamCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exams_SubjectId",
                table: "Exams",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_TeacherProfileId",
                table: "Exams",
                column: "TeacherProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttemptViolations_InstitutionId",
                table: "ExamAttemptViolations",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttemptQuestionSnapshots_InstitutionId",
                table: "ExamAttemptQuestionSnapshots",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttemptDraftAnswers_InstitutionId",
                table: "ExamAttemptDraftAnswers",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_ClassSectionId",
                table: "Attempts",
                column: "ClassSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_ExamId",
                table: "Attempts",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_InstitutionId_ExamId_StudentProfileId",
                table: "Attempts",
                columns: new[] { "InstitutionId", "ExamId", "StudentProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttemptAnswers_InstitutionId",
                table: "AttemptAnswers",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSections_GradeLevelId",
                table: "ClassSections",
                column: "GradeLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSections_InstitutionId",
                table: "ClassSections",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSections_SubjectId",
                table: "ClassSections",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSections_TeacherProfileId",
                table: "ClassSections",
                column: "TeacherProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeLevels_InstitutionId",
                table: "GradeLevels",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_SectionStudents_ClassSectionId",
                table: "SectionStudents",
                column: "ClassSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SectionStudents_InstitutionId_ClassSectionId_StudentProfileId",
                table: "SectionStudents",
                columns: new[] { "InstitutionId", "ClassSectionId", "StudentProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SectionStudents_StudentProfileId",
                table: "SectionStudents",
                column: "StudentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_GradeLevelId",
                table: "Subjects",
                column: "GradeLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_InstitutionId_Code",
                table: "Subjects",
                columns: new[] { "InstitutionId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_InstitutionId_TeacherCode",
                table: "Teachers",
                columns: new[] { "InstitutionId", "TeacherCode" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AttemptAnswers_Institutions_InstitutionId",
                table: "AttemptAnswers",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Attempts_ClassSections_ClassSectionId",
                table: "Attempts",
                column: "ClassSectionId",
                principalTable: "ClassSections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Attempts_Institutions_InstitutionId",
                table: "Attempts",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamAttemptDraftAnswers_Institutions_InstitutionId",
                table: "ExamAttemptDraftAnswers",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamAttemptQuestionSnapshots_Institutions_InstitutionId",
                table: "ExamAttemptQuestionSnapshots",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamAttemptViolations_Institutions_InstitutionId",
                table: "ExamAttemptViolations",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Exams_ClassSections_ClassSectionId",
                table: "Exams",
                column: "ClassSectionId",
                principalTable: "ClassSections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Exams_Institutions_InstitutionId",
                table: "Exams",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Exams_Subjects_SubjectId",
                table: "Exams",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Exams_Teachers_TeacherProfileId",
                table: "Exams",
                column: "TeacherProfileId",
                principalTable: "Teachers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Parents_Institutions_InstitutionId",
                table: "Parents",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ParentStudentLinks_Institutions_InstitutionId",
                table: "ParentStudentLinks",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Institutions_InstitutionId",
                table: "Questions",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Registrations_ClassSections_ClassSectionId",
                table: "Registrations",
                column: "ClassSectionId",
                principalTable: "ClassSections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Registrations_Institutions_InstitutionId",
                table: "Registrations",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_GradeLevels_GradeLevelId",
                table: "Students",
                column: "GradeLevelId",
                principalTable: "GradeLevels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Institutions_InstitutionId",
                table: "Students",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Teachers_TeacherProfileId",
                table: "Users",
                column: "TeacherProfileId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttemptAnswers_Institutions_InstitutionId",
                table: "AttemptAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_Attempts_ClassSections_ClassSectionId",
                table: "Attempts");

            migrationBuilder.DropForeignKey(
                name: "FK_Attempts_Institutions_InstitutionId",
                table: "Attempts");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamAttemptDraftAnswers_Institutions_InstitutionId",
                table: "ExamAttemptDraftAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamAttemptQuestionSnapshots_Institutions_InstitutionId",
                table: "ExamAttemptQuestionSnapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamAttemptViolations_Institutions_InstitutionId",
                table: "ExamAttemptViolations");

            migrationBuilder.DropForeignKey(
                name: "FK_Exams_ClassSections_ClassSectionId",
                table: "Exams");

            migrationBuilder.DropForeignKey(
                name: "FK_Exams_Institutions_InstitutionId",
                table: "Exams");

            migrationBuilder.DropForeignKey(
                name: "FK_Exams_Subjects_SubjectId",
                table: "Exams");

            migrationBuilder.DropForeignKey(
                name: "FK_Exams_Teachers_TeacherProfileId",
                table: "Exams");

            migrationBuilder.DropForeignKey(
                name: "FK_Parents_Institutions_InstitutionId",
                table: "Parents");

            migrationBuilder.DropForeignKey(
                name: "FK_ParentStudentLinks_Institutions_InstitutionId",
                table: "ParentStudentLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Institutions_InstitutionId",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_Registrations_ClassSections_ClassSectionId",
                table: "Registrations");

            migrationBuilder.DropForeignKey(
                name: "FK_Registrations_Institutions_InstitutionId",
                table: "Registrations");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_GradeLevels_GradeLevelId",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Institutions_InstitutionId",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Teachers_TeacherProfileId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "SectionStudents");

            migrationBuilder.DropTable(
                name: "ClassSections");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropTable(
                name: "Teachers");

            migrationBuilder.DropTable(
                name: "GradeLevels");

            migrationBuilder.DropIndex(
                name: "IX_Users_TeacherProfileId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Students_GradeLevelId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_InstitutionId_StudentCode",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_ClassSectionId",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_ExamId",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_InstitutionId_ExamId_StudentProfileId",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Questions_InstitutionId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_ParentStudentLinks_InstitutionId_ParentProfileId_StudentProfileId",
                table: "ParentStudentLinks");

            migrationBuilder.DropIndex(
                name: "IX_ParentStudentLinks_ParentProfileId",
                table: "ParentStudentLinks");

            migrationBuilder.DropIndex(
                name: "IX_Parents_InstitutionId_ParentCode",
                table: "Parents");

            migrationBuilder.DropIndex(
                name: "IX_Institutions_Name",
                table: "Institutions");

            migrationBuilder.DropIndex(
                name: "IX_Exams_ClassSectionId",
                table: "Exams");

            migrationBuilder.DropIndex(
                name: "IX_Exams_InstitutionId_ExamCode",
                table: "Exams");

            migrationBuilder.DropIndex(
                name: "IX_Exams_SubjectId",
                table: "Exams");

            migrationBuilder.DropIndex(
                name: "IX_Exams_TeacherProfileId",
                table: "Exams");

            migrationBuilder.DropIndex(
                name: "IX_ExamAttemptViolations_InstitutionId",
                table: "ExamAttemptViolations");

            migrationBuilder.DropIndex(
                name: "IX_ExamAttemptQuestionSnapshots_InstitutionId",
                table: "ExamAttemptQuestionSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_ExamAttemptDraftAnswers_InstitutionId",
                table: "ExamAttemptDraftAnswers");

            migrationBuilder.DropIndex(
                name: "IX_Attempts_ClassSectionId",
                table: "Attempts");

            migrationBuilder.DropIndex(
                name: "IX_Attempts_ExamId",
                table: "Attempts");

            migrationBuilder.DropIndex(
                name: "IX_Attempts_InstitutionId_ExamId_StudentProfileId",
                table: "Attempts");

            migrationBuilder.DropIndex(
                name: "IX_AttemptAnswers_InstitutionId",
                table: "AttemptAnswers");

            migrationBuilder.DropColumn(
                name: "TeacherProfileId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "GradeLevelId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "ClassSectionId",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "ParentStudentLinks");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "Parents");

            migrationBuilder.DropColumn(
                name: "ExamManagementMode",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "ClassSectionId",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "TeacherProfileId",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "ExamAttemptViolations");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "ExamAttemptQuestionSnapshots");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "ExamAttemptDraftAnswers");

            migrationBuilder.DropColumn(
                name: "ClassSectionId",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "AttemptAnswers");

            migrationBuilder.CreateIndex(
                name: "IX_Students_StudentCode",
                table: "Students",
                column: "StudentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_ExamId_StudentProfileId",
                table: "Registrations",
                columns: new[] { "ExamId", "StudentProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentStudentLinks_ParentProfileId_StudentProfileId",
                table: "ParentStudentLinks",
                columns: new[] { "ParentProfileId", "StudentProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parents_ParentCode",
                table: "Parents",
                column: "ParentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exams_ExamCode",
                table: "Exams",
                column: "ExamCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_ExamId_StudentProfileId",
                table: "Attempts",
                columns: new[] { "ExamId", "StudentProfileId" },
                unique: true);
        }
    }
}
