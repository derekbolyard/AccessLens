using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessLensApi.Migrations
{
    /// <inheritdoc />
    public partial class NotSure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sites_Users_UserId",
                table: "Sites");

            migrationBuilder.DropIndex(
                name: "IX_Sites_UserId",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Sites");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Sites",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_Email",
                table: "Sites",
                column: "Email");

            migrationBuilder.AddForeignKey(
                name: "FK_Sites_Users_Email",
                table: "Sites",
                column: "Email",
                principalTable: "Users",
                principalColumn: "Email",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sites_Users_Email",
                table: "Sites");

            migrationBuilder.DropIndex(
                name: "IX_Sites_Email",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Sites");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Sites",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_UserId",
                table: "Sites",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sites_Users_UserId",
                table: "Sites",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Email",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
