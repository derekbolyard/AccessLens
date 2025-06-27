using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessLensApi.Migrations
{
    /// <inheritdoc />
    public partial class TrackUrlFailure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "ScannedUrls",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HttpStatusCode",
                table: "ScannedUrls",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxRetries",
                table: "ScannedUrls",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "ScannedUrls",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ScanDurationMs",
                table: "ScannedUrls",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "ScannedUrls");

            migrationBuilder.DropColumn(
                name: "HttpStatusCode",
                table: "ScannedUrls");

            migrationBuilder.DropColumn(
                name: "MaxRetries",
                table: "ScannedUrls");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "ScannedUrls");

            migrationBuilder.DropColumn(
                name: "ScanDurationMs",
                table: "ScannedUrls");
        }
    }
}
