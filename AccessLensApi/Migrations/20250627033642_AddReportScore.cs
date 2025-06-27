using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessLensApi.Migrations
{
    /// <inheritdoc />
    public partial class AddReportScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "Reports",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Score",
                table: "Reports");
        }
    }
}
