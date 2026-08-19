using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncTenantScopeAfterReset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassSections_Teachers_TeacherProfileId",
                table: "ClassSections");

            migrationBuilder.DropForeignKey(
                name: "FK_Exams_Teachers_TeacherProfileId",
                table: "Exams");

            migrationBuilder.DropForeignKey(
                name: "FK_Teachers_Institutions_InstitutionId",
                table: "Teachers");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Teachers_TeacherProfileId",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Teachers",
                table: "Teachers");

            migrationBuilder.RenameTable(
                name: "Teachers",
                newName: "TeacherProfile");

            migrationBuilder.RenameIndex(
                name: "IX_Teachers_InstitutionId_TeacherCode",
                table: "TeacherProfile",
                newName: "IX_TeacherProfile_InstitutionId_TeacherCode");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TeacherProfile",
                table: "TeacherProfile",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Students_InstitutionId",
                table: "Students",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_InstitutionId",
                table: "Registrations",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Parents_InstitutionId",
                table: "Parents",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_InstitutionId",
                table: "Exams",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_InstitutionId",
                table: "Attempts",
                column: "InstitutionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSections_TeacherProfile_TeacherProfileId",
                table: "ClassSections",
                column: "TeacherProfileId",
                principalTable: "TeacherProfile",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Exams_TeacherProfile_TeacherProfileId",
                table: "Exams",
                column: "TeacherProfileId",
                principalTable: "TeacherProfile",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherProfile_Institutions_InstitutionId",
                table: "TeacherProfile",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_TeacherProfile_TeacherProfileId",
                table: "Users",
                column: "TeacherProfileId",
                principalTable: "TeacherProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassSections_TeacherProfile_TeacherProfileId",
                table: "ClassSections");

            migrationBuilder.DropForeignKey(
                name: "FK_Exams_TeacherProfile_TeacherProfileId",
                table: "Exams");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherProfile_Institutions_InstitutionId",
                table: "TeacherProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_TeacherProfile_TeacherProfileId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Students_InstitutionId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_InstitutionId",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Parents_InstitutionId",
                table: "Parents");

            migrationBuilder.DropIndex(
                name: "IX_Exams_InstitutionId",
                table: "Exams");

            migrationBuilder.DropIndex(
                name: "IX_Attempts_InstitutionId",
                table: "Attempts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TeacherProfile",
                table: "TeacherProfile");

            migrationBuilder.RenameTable(
                name: "TeacherProfile",
                newName: "Teachers");

            migrationBuilder.RenameIndex(
                name: "IX_TeacherProfile_InstitutionId_TeacherCode",
                table: "Teachers",
                newName: "IX_Teachers_InstitutionId_TeacherCode");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Teachers",
                table: "Teachers",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSections_Teachers_TeacherProfileId",
                table: "ClassSections",
                column: "TeacherProfileId",
                principalTable: "Teachers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Exams_Teachers_TeacherProfileId",
                table: "Exams",
                column: "TeacherProfileId",
                principalTable: "Teachers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Teachers_Institutions_InstitutionId",
                table: "Teachers",
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
    }
}
