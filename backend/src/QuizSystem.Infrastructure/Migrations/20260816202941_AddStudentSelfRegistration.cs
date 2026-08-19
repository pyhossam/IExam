using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentSelfRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentAccountRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Gender = table.Column<string>(type: "TEXT", nullable: false),
                    EducationStage = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    EmailVerificationTokenHash = table.Column<string>(type: "TEXT", nullable: true),
                    EmailVerificationTokenExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EmailVerifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DecidedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DecidedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RejectionReason = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAccountRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentAccountRequests_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentAccountRequests_Email",
                table: "StudentAccountRequests",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentAccountRequests_InstitutionId_Status",
                table: "StudentAccountRequests",
                columns: new[] { "InstitutionId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentAccountRequests");
        }
    }
}
