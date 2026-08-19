using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddParentUserRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_ParentProfileId",
                table: "Users");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Parents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ParentProfileId",
                table: "Users",
                column: "ParentProfileId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_ParentProfileId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Parents");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ParentProfileId",
                table: "Users",
                column: "ParentProfileId");
        }
    }
}
