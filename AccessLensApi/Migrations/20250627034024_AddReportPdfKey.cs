using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessLensApi.Migrations
{
    /// <inheritdoc />
    public partial class AddReportPdfKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PdfKey",
                table: "Reports",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PdfKey",
                table: "Reports");
        }
    }
}
